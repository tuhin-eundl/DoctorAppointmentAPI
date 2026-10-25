using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoctorAppointment.Service.MCPTools;

public record ScheduleDayOverride(
    [property: JsonPropertyName("date"), Description("The date to override, in yyyy-MM-dd format. Must be within the schedule range.")] string Date,
    [property: JsonPropertyName("startTime"), Description("Start time for this date in HH:mm format (e.g. 10:00)")] string StartTime,
    [property: JsonPropertyName("endTime"), Description("End time for this date in HH:mm format (e.g. 14:00)")] string EndTime
);

public record ScheduleRequest(
    [property: JsonPropertyName("fromDate"), Description("First date of the schedule, in yyyy-MM-dd format")] string FromDate,
    [property: JsonPropertyName("toDate"), Description("Last date of the schedule (inclusive), in yyyy-MM-dd format. Use the same value as fromDate for a single day.")] string ToDate,
    [property: JsonPropertyName("dailyStartTime"), Description("Default start time applied to every date in the range, in HH:mm format (e.g. 09:00)")] string DailyStartTime,
    [property: JsonPropertyName("dailyEndTime"), Description("Default end time applied to every date in the range, in HH:mm format (e.g. 17:00)")] string DailyEndTime,
    [property: JsonPropertyName("maxAppointmentsPerDay"), Description("How many patients can book each day, 1-200. Default 20.")] int MaxAppointmentsPerDay = 20,
    [property: JsonPropertyName("averageAppointmentMinutes"), Description("Average length of one appointment in minutes, 5-240. Default 15.")] int AverageAppointmentMinutes = 15,
    [property: JsonPropertyName("dayOverrides"), Description("Optional: specific dates that use different times than the daily default. Leave empty if every day has the same hours.")] List<ScheduleDayOverride>? DayOverrides = null
);

[McpServerToolType]
public sealed class DoctorScheduleTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/doctor-schedules";

    [KernelFunction, Description(
        "Returns the schedules of the logged-in doctor, including each schedule's scheduleId, date range, " +
        "capacity and scheduleDays (with scheduleDayId, date, startTime, endTime). " +
        "Use this first when a doctor asks about, changes or deletes their own schedule. Requires the signed-in user to be a Doctor.")]
    [McpServerTool]
    public async Task<JsonElement> GetMySchedules(CancellationToken cancellationToken = default)
    {
        var doctorId = HttpHandler.GetCurrentUserId(httpContextAccessor)
            ?? throw new InvalidOperationException("The current request is not authenticated.");

        return await GetSchedulesByDoctorId(doctorId, cancellationToken);
    }

    [KernelFunction, Description(
        "Returns every doctor schedule in the system (not scoped to the caller). " +
        "Prefer GetMySchedules or GetSchedulesByDoctorId when the doctor is known. Requires authorization.")]
    [McpServerTool]
    public async Task<JsonElement> GetAllSchedules(
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            BasePath,
            cancellationToken);

        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Returns all schedules of a specific doctor by doctor ID, including scheduleDays. " +
        "To check which time slots a patient can book, prefer the Appointments GetDoctorAvailability tool instead. " +
        "Requires authorization.")]
    [McpServerTool]
    public async Task<JsonElement> GetSchedulesByDoctorId(
        [Description("The ID of the doctor whose schedules should be returned")] int doctorId,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            $"{BasePath}/doctor/{doctorId}",
            cancellationToken);

        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Returns a single schedule (including its scheduleDays) by schedule ID. Requires authorization.")]
    [McpServerTool]
    public async Task<JsonElement> GetScheduleById(
        [Description("The ID of the schedule to retrieve")] int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            $"{BasePath}/{scheduleId}",
            cancellationToken);

        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Creates a schedule for the logged-in doctor covering every date from fromDate to toDate. " +
        "Give one daily start/end time; every date in the range is filled automatically. " +
        "Use dayOverrides only for dates with different hours. Dates must not overlap an existing schedule. " +
        "Requires the signed-in user to be a Doctor.")]
    [McpServerTool]
    public async Task<JsonElement> CreateSchedule(
        [Description("The schedule to create")] ScheduleRequest schedule,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildPayload(schedule);
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PostAsJsonAsync(
            BasePath,
            payload,
            cancellationToken);

        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Replaces an existing schedule of the logged-in doctor with a new date range, daily hours and capacity. " +
        "Every date in the new range is filled automatically; use dayOverrides for dates with different hours. " +
        "Get the scheduleId from GetMySchedules first. Requires the signed-in user to be a Doctor.")]
    [McpServerTool]
    public async Task<JsonElement> UpdateSchedule(
        [Description("The ID of the schedule to update")] int scheduleId,
        [Description("The full new schedule details")] ScheduleRequest schedule,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildPayload(schedule);
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PutAsJsonAsync(
            $"{BasePath}/{scheduleId}",
            payload,
            cancellationToken);

        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Deletes an entire schedule of the logged-in doctor, including all of its days. " +
        "Always confirm with the user first. Requires the signed-in user to be a Doctor.")]
    [McpServerTool]
    public async Task<string> DeleteSchedule(
        [Description("The ID of the schedule to delete")] int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.DeleteAsync(
            $"{BasePath}/{scheduleId}",
            cancellationToken);

        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return $"Schedule {scheduleId} deleted successfully.";
    }

    [KernelFunction, Description(
        "Removes a single day from a schedule of the logged-in doctor (e.g. a day off). " +
        "Fails if that day has pending or approved appointments. If it was the last day, the whole schedule is removed. " +
        "Get scheduleId and scheduleDayId from GetMySchedules first. Always confirm with the user first. Requires the signed-in user to be a Doctor.")]
    [McpServerTool]
    public async Task<string> DeleteScheduleDay(
        [Description("The ID of the parent schedule")] int scheduleId,
        [Description("The ID of the schedule day to remove")] int scheduleDayId,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.DeleteAsync(
            $"{BasePath}/{scheduleId}/days/{scheduleDayId}",
            cancellationToken);

        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return $"Schedule day {scheduleDayId} deleted from schedule {scheduleId}.";
    }

    private static object BuildPayload(ScheduleRequest request)
    {
        var fromDate = ParseDate(request.FromDate, "fromDate");
        var toDate = ParseDate(request.ToDate, "toDate");
        if (fromDate > toDate)
        {
            throw new ArgumentException("fromDate must be on or before toDate.");
        }

        var defaultStart = ParseTime(request.DailyStartTime, "dailyStartTime");
        var defaultEnd = ParseTime(request.DailyEndTime, "dailyEndTime");

        var overrides = new Dictionary<DateOnly, (TimeOnly Start, TimeOnly End)>();
        foreach (var dayOverride in request.DayOverrides ?? [])
        {
            var date = ParseDate(dayOverride.Date, "dayOverrides.date");
            if (date < fromDate || date > toDate)
            {
                throw new ArgumentException($"Override date {date:yyyy-MM-dd} is outside the schedule range.");
            }

            overrides[date] = (ParseTime(dayOverride.StartTime, "dayOverrides.startTime"),
                ParseTime(dayOverride.EndTime, "dayOverrides.endTime"));
        }

        // The API requires exactly one entry for every date in the range, so expand it here.
        var scheduleDays = new List<object>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var (start, end) = overrides.TryGetValue(date, out var times) ? times : (defaultStart, defaultEnd);
            scheduleDays.Add(new
            {
                date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                startTime = start.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                endTime = end.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
            });
        }

        return new
        {
            fromDate = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            toDate = toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            scheduleDays,
            maxAppointmentsPerDay = request.MaxAppointmentsPerDay,
            averageAppointmentMinutes = request.AverageAppointmentMinutes
        };
    }

    private static DateOnly ParseDate(string value, string field) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date)
            ? date
            : throw new ArgumentException($"{field} '{value}' is not a valid date. Use yyyy-MM-dd.");

    private static TimeOnly ParseTime(string value, string field) =>
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var time)
            ? time
            : throw new ArgumentException($"{field} '{value}' is not a valid time. Use HH:mm.");
}

