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
    public async IAsyncEnumerable<string> ChatStream([FromBody] ChatRequest request)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage(@"
            You are an AI assistant capable of managing system users. 
            You have access to tool actions: creating, retrieving, updating, and deleting users.
            Always confirm before performing destructive operations like deleting users.
        ");
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
            kernel: _kernel
        );

        await foreach (var chunk in streamingResponse)
        {
            if (chunk.Content is not null)
            {
                yield return chunk.Content;
            }
        }
    }
}

