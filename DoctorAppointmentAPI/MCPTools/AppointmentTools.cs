using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoctorAppointment.Service.MCPTools;

public record BookAppointmentRequest(
    [property: JsonPropertyName("scheduleDayId"), Description("The scheduleDayId of the chosen day, taken from GetDoctorAvailability")] int ScheduleDayId,
    [property: JsonPropertyName("startTime"), Description("The start time of a Free slot on that day, in HH:mm:ss format (e.g. 09:30:00)")] string StartTime,
    [property: JsonPropertyName("reason"), Description("Optional reason for the visit (max 500 characters)")] string? Reason = null
);

[McpServerToolType]
public sealed class AppointmentTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/appointments";

    [KernelFunction, Description(
        "Lists all doctors with id, firstName, lastName, title, department and nextAvailableDate (null if fully booked). " +
        "Use it to find a doctor's id by name or department before calling GetDoctorAvailability.")]
    [McpServerTool]
    public async Task<JsonElement> GetBookableDoctors(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync($"{BasePath}/doctors", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Returns a doctor's upcoming bookable days. Each day has scheduleDayId, date, startTime, endTime, " +
        "averageAppointmentMinutes, capacity, booked, freeSlots and a list of slots (time, state: Free, Booked or Unavailable). " +
        "Only Free slots can be booked.")]
    [McpServerTool]
    public async Task<JsonElement> GetDoctorAvailability(
        [Description("The ID of the doctor")] int doctorId,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync($"{BasePath}/doctors/{doctorId}/availability", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Books an appointment for the signed-in patient on a doctor's schedule day at a Free time slot. " +
        "The appointment starts as Pending and the doctor is notified to approve it. Only Patients can book, " +
        "and only one active appointment per doctor per day. " +
        "Use GetDoctorAvailability first to find the scheduleDayId and a Free startTime.")]
    [McpServerTool]
    public async Task<JsonElement> BookAppointment(
        [Description("The booking details")] BookAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PostAsJsonAsync(BasePath, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Returns the signed-in user's appointments (a patient's own bookings, or a doctor's bookings), newest date first. " +
        "Each item has id, date, startTime, endTime, serialNumber, status (Pending, Approved, Declined, Cancelled), " +
        "cancelledBy, reason, requestedAt, and doctor/patient details (id, name, title, department, age, gender). " +
        "Use it to find an appointment id before changing its status.")]
    [McpServerTool]
    public async Task<JsonElement> GetMyAppointments(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(BasePath, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Changes an appointment's status. Allowed changes: a Doctor can set a Pending appointment to Approved or Declined, " +
        "and an Approved one to Cancelled; a Patient can set a Pending or Approved appointment to Cancelled. " +
        "Past appointments can't be cancelled. Get the appointment id from GetMyAppointments. " +
        "Always confirm with the user before declining or cancelling.")]
    [McpServerTool]
    public async Task<JsonElement> UpdateAppointmentStatus(
        [Description("The ID of the appointment")] int appointmentId,
        [Description("The new status. Allowed values: Approved, Declined, Cancelled")] string status,
        [Description("Optional. The doctor's reason when declining or cancelling, shown to the patient")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{appointmentId}/status",
            new { status, reason },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"API returned {(int)response.StatusCode} ({response.StatusCode}): {body}",
            null,
            response.StatusCode);
    }
}
