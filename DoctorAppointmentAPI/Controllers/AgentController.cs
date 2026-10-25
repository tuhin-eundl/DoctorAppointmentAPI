using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DoctorAppointment.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly Kernel _kernel;

    public AgentController(Kernel kernel)
    {
        _kernel = kernel;
    }

    public record ChatRequest(string UserPrompt);

    [HttpPost("chat-stream")]
    [Produces("text/plain")]
    [Authorize]
    public async Task ChatStream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

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
            Never ask for usernames or passwords to sign in. Booking requires a Patient account; schedule actions require a Doctor account.
            If a tool fails with 401 or 403, explain that the signed-in user is not allowed to do that action.
            Always confirm before performing destructive operations like deleting users or cancelling appointments.
            Never reveal access tokens or passwords in your responses.
        ");
        history.AddSystemMessage($"Today's date is {DateTime.Now:yyyy-MM-dd} ({DateTime.Now:dddd}).");
        history.AddUserMessage(request.UserPrompt);

        // Configure Auto Invocation so LLM can invoke UserTools methods automatically
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // Auto-execute tools
            Temperature = 0.2
        };

        // Stream the response back
        var streamingResponse = chatService.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings: executionSettings,
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
}

