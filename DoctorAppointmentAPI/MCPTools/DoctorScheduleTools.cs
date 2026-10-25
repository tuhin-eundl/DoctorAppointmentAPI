using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DoctorAppointment.Service.MCPTools;

[McpServerToolType]
public sealed class DoctorScheduleTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/doctor-schedules";

    [McpServerTool]
    [Description(
        "Returns every doctor schedule in the system. " +
        "Requires an authenticated user; results are NOT scoped to the caller.")]
    public async Task<JsonElement> GetAllSchedules(
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            BasePath,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description(
        "Returns all schedules that belong to the given doctor. " +
        "Each item contains the schedule plus its `scheduleDays` collection.")]
    public async Task<JsonElement> GetSchedulesByDoctorId(
        [Description("The ID of the doctor whose schedules should be returned")] int doctorId,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            $"{BasePath}/doctor/{doctorId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description(
        "Returns a single doctor schedule (including its `scheduleDays`) by schedule ID.")]
    public async Task<JsonElement> GetScheduleById(
        [Description("The ID of the schedule to retrieve")] int scheduleId,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            $"{BasePath}/{scheduleId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description(
        "Creates a new schedule for the currently authenticated doctor. " +
        "The doctor ID is taken from the bearer token, so it is NOT a parameter. " +
        "Returns the created schedule.")]
    public async Task<JsonElement> CreateSchedule(
        [Description(
            "Schedule payload as JSON. Shape: the API's `DoctorScheduleDto` " +
            "(e.g. start/end date range plus a `scheduleDays` array of daily timings).")]
        JsonElement schedule,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PostAsJsonAsync(
            BasePath,
            schedule,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description(
        "Updates an existing schedule owned by the currently authenticated doctor. " +
        "Returns the updated schedule.")]
    public async Task<JsonElement> UpdateSchedule(
        [Description("The ID of the schedule to update")] int scheduleId,
        [Description("Updated schedule payload as JSON, matching the API's `UpdateDoctorScheduleDto`.")]
        JsonElement schedule,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PutAsJsonAsync(
            $"{BasePath}/{scheduleId}",
            schedule,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description(
        "Deletes an entire schedule owned by the currently authenticated doctor. " +
        "All of its schedule days are removed as well.")]
    public async Task<string> DeleteSchedule(
        [Description("The ID of the schedule to delete")] int scheduleId,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.DeleteAsync(
            $"{BasePath}/{scheduleId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return $"Schedule {scheduleId} deleted successfully.";
    }

    [McpServerTool]
    [Description(
        "Deletes a single day from a schedule owned by the currently authenticated doctor. " +
        "If the removed day was the last active day in the schedule, " +
        "the parent schedule is automatically deleted as well.")]
    public async Task<string> DeleteScheduleDay(
        [Description("The ID of the parent schedule")] int scheduleId,
        [Description("The ID of the schedule day to remove")] int scheduleDayId,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.DeleteAsync(
            $"{BasePath}/{scheduleId}/days/{scheduleDayId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return $"Schedule day {scheduleDayId} deleted from schedule {scheduleId}.";
    }
}

