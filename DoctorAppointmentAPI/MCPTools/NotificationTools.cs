using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DoctorAppointment.Service.MCPTools;

[McpServerToolType]
public sealed class NotificationTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/notifications";

    [KernelFunction, Description(
        "Returns the signed-in user's notifications, newest first. Each item has id, message, isRead and createdAt (UTC). " +
        "Patients are notified when a doctor approves, declines or cancels their appointment, or when their request is closed " +
        "because the doctor confirmed another patient for that time. Doctors are notified when a patient books, withdraws or cancels. " +
        "Use this when the user asks about notifications, alerts, or what's new.")]
    [McpServerTool]
    public async Task<JsonElement> GetMyNotifications(
        [Description("true to return only unread notifications; false (default) for all")] bool unreadOnly = false,
        [Description("Maximum number of notifications to return, 1-200. Default 20.")] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            $"{BasePath}?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}&take={Math.Clamp(take, 1, 200)}",
            cancellationToken);
        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Marks one of the signed-in user's notifications as read. Get the notification id from GetMyNotifications. " +
        "Call it after showing an unread notification to the user, or when they ask to mark it as read.")]
    [McpServerTool]
    public async Task<string> MarkNotificationAsRead(
        [Description("The id of the notification")] long notificationId,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PatchAsync($"{BasePath}/{notificationId}/read", null, cancellationToken);
        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return $"Notification {notificationId} marked as read.";
    }
}
