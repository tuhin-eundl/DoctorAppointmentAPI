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
        "Lists doctors that patients can book, with id, name, title, department and next available date. " +
        "Requires authorization.")]
    [McpServerTool]
    public async Task<JsonElement> GetBookableDoctors(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync($"{BasePath}/doctors", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Returns a doctor's upcoming bookable days. Each day has a scheduleDayId, date, capacity, free slot count " +
        "and a list of time slots with state Free, Booked or Unavailable. Only Free slots can be booked. " +
        "Requires authorization.")]
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
        "Books an appointment for the logged-in patient on a doctor's schedule day at a Free time slot. " +
        "The appointment starts as Pending until the doctor approves it. Only users with the Patient role can book. " +
        "Use GetDoctorAvailability first to find the scheduleDayId and a Free startTime. Requires authorization.")]
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
        "Returns the logged-in user's appointments (a patient's own bookings, or a doctor's bookings), newest first. " +
        "Requires authorization.")]
    [McpServerTool]
    public async Task<JsonElement> GetMyAppointments(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(BasePath, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description(
        "Changes an appointment's status. Doctors can set Approved or Declined; patients and doctors can set Cancelled. " +
        "Always confirm with the user before cancelling. Requires authorization.")]
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
