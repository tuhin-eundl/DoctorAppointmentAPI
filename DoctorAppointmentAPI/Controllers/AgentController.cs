using System.ClientModel;
using System.Net;
using DoctorAppointment.Service.Agent;
using DoctorAppointment.Service.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

namespace DoctorAppointment.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    // Enough context for follow-ups and confirmations without sending an ever-growing history.
    private const int MaxMessages = 30;
    private const int MaxMessageLength = 4000;

    private readonly Kernel _kernel;
    private readonly ILogger<AgentController> _logger;
    private readonly string? _reasoningEffort;

    public AgentController(Kernel kernel, ILogger<AgentController> logger, IConfiguration configuration)
    {
        _kernel = kernel;
        _logger = logger;
        _reasoningEffort = configuration["AiSettings:ReasoningEffort"];
    }

    public record ChatRequest(string UserPrompt);

    /// <summary>One turn of the conversation. Role is "user" or "assistant".</summary>
    public record AgentMessage(string Role, string Content);

    public record AgentChatRequest(List<AgentMessage> Messages);

    // Configure Auto Invocation so LLM can invoke the tool plugins automatically
    // Local thinking models (e.g. Qwen on Ollama) can put the final answer in the reasoning field and
    // leave the reply empty after a tool call, so AiSettings:ReasoningEffort = "none" turns thinking off.
    private OpenAIPromptExecutionSettings ExecutionSettings => new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // Auto-execute tools
        Temperature = 0.2,
#pragma warning disable OPENAI001 // ChatReasoningEffortLevel is marked experimental by the OpenAI SDK
        ReasoningEffort = string.IsNullOrWhiteSpace(_reasoningEffort)
            ? null
            : new ChatReasoningEffortLevel(_reasoningEffort)
#pragma warning restore OPENAI001
    };

    [HttpPost("chat-stream")]
    [Produces("text/plain")]
    [Authorize]
    public async Task ChatStream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        var history = CreateHistory();
        history.AddUserMessage(request.UserPrompt);

        // Stream the response back
        var streamingResponse = chatService.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings: ExecutionSettings,
            kernel: _kernel,
            cancellationToken: cancellationToken
        );

        Response.ContentType = "text/plain; charset=utf-8";

        await foreach (var chunk in streamingResponse)
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                await Response.WriteAsync(chunk.Content, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Runs the assistant over the conversation so far and streams its progress and reply as
    /// newline-delimited JSON events (see <see cref="AgentEventWriter"/>). The last message must be the user's.
    /// </summary>
    /// <remarks>
    /// No [Produces]: it would apply to the JSON validation errors too and turn them into 406s.
    /// The stream sets its own content type.
    /// </remarks>
    [HttpPost("chat")]
    [Authorize]
    public async Task<IActionResult> Chat([FromBody] AgentChatRequest request, CancellationToken cancellationToken)
    {
        var messages = (request.Messages ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(MaxMessages)
            .ToList();

        if (messages.Count == 0 || !IsRole(messages[^1], "user"))
        {
            return BadRequest(new ErrorResponse("Send at least one message, ending with the user's.", null));
        }
        if (messages.Any(m => !IsRole(m, "user") && !IsRole(m, "assistant")))
        {
            return BadRequest(new ErrorResponse("Message roles must be \"user\" or \"assistant\".", null));
        }

        // Events must reach the app as they happen, not when the response completes.
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var events = new AgentEventWriter(Response);
        var progress = new AgentProgressFilter(events, _logger);
        // The Kernel is transient, so this filter only sees this request's tool calls.
        _kernel.AutoFunctionInvocationFilters.Add(progress);

        try
        {
            await events.StatusAsync("Initializing your assistant", cancellationToken);

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var history = CreateHistory();
            foreach (var message in messages)
            {
                var content = message.Content.Length > MaxMessageLength
                    ? message.Content[..MaxMessageLength]
                    : message.Content;

                if (IsRole(message, "user"))
                {
                    history.AddUserMessage(content);
                }
                else
                {
                    history.AddAssistantMessage(content);
                }
            }

            await events.StatusAsync("Understanding your request", cancellationToken);

            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
                               history,
                               executionSettings: ExecutionSettings,
                               kernel: _kernel,
                               cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    await events.TextAsync(chunk.Content, cancellationToken);
                }
            }

            await events.DoneAsync(progress.Changed, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The user pressed stop or closed the app; nobody is left to tell.
        }
        catch (Exception ex)
        {
            // The response has already started, so ExceptionMiddleware can't send an error; report it in-stream.
            _logger.LogError(ex, "Agent chat failed: {Message}", ex.Message);
            await events.ErrorAsync(FriendlyError(ex), progress.Changed, CancellationToken.None);
        }

        return new EmptyResult();
    }

    private static bool IsRole(AgentMessage message, string role) =>
        string.Equals(message.Role, role, StringComparison.OrdinalIgnoreCase);

    private static string FriendlyError(Exception exception) =>
        exception switch
        {
            HttpOperationException { StatusCode: HttpStatusCode.TooManyRequests }
                or ClientResultException { Status: 429 } =>
                "The assistant is handling a lot of requests right now. Please try again in a moment.",
            _ => "Something went wrong while I was working on that. Please try again."
        };

    private static ChatHistory CreateHistory()
    {
        var history = new ChatHistory();
        history.AddSystemMessage(@"
            You are an AI assistant for a doctor appointment system. You can manage system users and book appointments.
            You have access to tool actions: creating, retrieving, updating, and deleting users;
            listing bookable doctors, checking a doctor's availability, booking appointments, listing the user's appointments,
            and changing appointment status.
            To book an appointment for the signed-in patient: find the doctor with GetBookableDoctors, call GetDoctorAvailability,
            pick the scheduleDayId and a Free slot matching the requested date/time, then call BookAppointment.
            If the requested slot is not Free, suggest the nearest Free slots instead of guessing.
            Doctors can manage their own schedules: use GetMySchedules to see them (and to find scheduleId / scheduleDayId),
            CreateSchedule or UpdateSchedule with a date range and one daily start/end time (only use dayOverrides for days
            with different hours), DeleteScheduleDay for a single day off, and DeleteSchedule for a whole schedule.
            The user is already signed in; every tool runs as that user with their role and permissions.
            Before doing anything else, call GetCurrentUser to learn who the user is and whether they are a Doctor or a Patient,
            and address them by name. Only offer actions that fit their user type.
            Never ask for usernames or passwords to sign in. Booking requires a Patient account; schedule actions require a Doctor account.
            Use GetMyNotifications for notifications or 'what's new' questions (doctors are notified of new bookings awaiting approval),
            and MarkNotificationAsRead after the user has seen one. Use GetHospitals to resolve hospital names to hospitalIds.
            If a tool fails with 401 or 403, explain that their account isn't allowed to do that instead of retrying.
            Always confirm before performing destructive operations like deleting users or cancelling appointments.
            Never reveal access tokens or passwords in your responses.
            Resolve relative dates such as 'tomorrow' or 'next Monday' from today's date. If something essential is missing
            or ambiguous (which doctor, which day), ask one short follow-up question instead of guessing.
            You are replying in a small chat window: keep answers short and friendly, use plain sentences and simple '-' bullet
            lists, and no tables or headings. After completing an action, state clearly what was done (doctor, date, time,
            status), and show times in 12-hour format such as 9:30 AM.
        ");
        history.AddSystemMessage($"Today's date is {DateTime.Now:yyyy-MM-dd} ({DateTime.Now:dddd}).");
        return history;
    }
}
