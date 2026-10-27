using System.Text.Json;

namespace DoctorAppointment.Service.Agent;

/// <summary>
/// Streams the agent's progress to the app as newline-delimited JSON: one event object per line,
/// flushed right away so the app can show what the agent is doing while it is still working.
/// </summary>
/// <remarks>
/// Event shapes (mirrored by AgentEvent in the app's src/lib/agent.ts):
///  { type: "status", message }                        what the agent is doing between steps
///  { type: "step", id, status, label }                a tool call; status is running, done or failed
///  { type: "text", delta }                            the next piece of the reply
///  { type: "done", changed }                          finished; changed lists the data areas it modified
///  { type: "error", message, changed }                stopped by an error
/// </remarks>
public sealed class AgentEventWriter(HttpResponse response)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Tool calls and reply text are written from the same streaming loop, but guard against overlap anyway.
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public Task StatusAsync(string message, CancellationToken cancellationToken) =>
        WriteAsync(new { type = "status", message }, cancellationToken);

    public Task StepAsync(int id, string status, string label, CancellationToken cancellationToken) =>
        WriteAsync(new { type = "step", id, status, label }, cancellationToken);

    public Task TextAsync(string delta, CancellationToken cancellationToken) =>
        WriteAsync(new { type = "text", delta }, cancellationToken);

    public Task DoneAsync(IReadOnlyCollection<string> changed, CancellationToken cancellationToken) =>
        WriteAsync(new { type = "done", changed }, cancellationToken);

    public Task ErrorAsync(string message, IReadOnlyCollection<string> changed, CancellationToken cancellationToken) =>
        WriteAsync(new { type = "error", message, changed }, cancellationToken);

    private async Task WriteAsync(object payload, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(payload, JsonOptions) + "\n";

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await response.WriteAsync(line, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
