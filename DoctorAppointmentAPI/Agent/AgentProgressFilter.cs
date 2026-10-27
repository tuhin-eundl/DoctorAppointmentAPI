using Microsoft.SemanticKernel;

namespace DoctorAppointment.Service.Agent;

/// <summary>
/// Reports each tool the model invokes as a progress step (running, then done or failed),
/// and remembers which data areas were changed so the app can refresh them afterwards.
/// One instance per chat request.
/// </summary>
public sealed class AgentProgressFilter(AgentEventWriter events, ILogger logger) : IAutoFunctionInvocationFilter
{
    private readonly HashSet<string> _changed = [];
    private int _lastStepId;

    public IReadOnlyCollection<string> Changed => _changed;

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        var stepId = Interlocked.Increment(ref _lastStepId);
        var progress = AgentToolLabels.For(context.Function.Name, context.Arguments);
        var cancellationToken = context.CancellationToken;

        await events.StepAsync(stepId, "running", progress.Running, cancellationToken);

        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Semantic Kernel hands the error back to the model, which explains it to the user.
            logger.LogWarning(ex, "Agent tool {Function} failed: {Message}", context.Function.Name, ex.Message);
            await events.StepAsync(stepId, "failed", progress.Running, cancellationToken);
            throw;
        }

        if (progress.Changes is not null)
        {
            _changed.Add(progress.Changes);
        }

        await events.StepAsync(stepId, "done", progress.Done, cancellationToken);
        // Shown until the model calls the next tool or starts writing its reply.
        await events.StatusAsync("Reviewing the results", cancellationToken);
    }
}
