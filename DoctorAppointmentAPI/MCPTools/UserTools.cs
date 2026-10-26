using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoctorAppointment.Service.MCPTools;

#region DTOs for Tool Schema Binding

public record UserDto(
    [property: JsonPropertyName("id"), Description("The unique user ID")] int Id,
    [property: JsonPropertyName("firstName"), Description("The user's first name")] string FirstName,
    [property: JsonPropertyName("lastName"), Description("The user's last name")] string? LastName,
    [property: JsonPropertyName("title"), Description("The user's title (e.g., Dr., Mr., Ms.)")] string? Title,
    [property: JsonPropertyName("username"), Description("The user's login username")] string Username,
    [property: JsonPropertyName("email"), Description("The user's email address")] string? Email,
    [property: JsonPropertyName("age"), Description("The user's age in years")] int? Age,
    [property: JsonPropertyName("gender"), Description("The user's gender")] string? Gender,
    [property: JsonPropertyName("role"), Description("The user's system role: Patient or Doctor")] string Role,
    [property: JsonPropertyName("department"), Description("The doctor's department")] string? Department
);

public record CreateUserRequest(
    [property: JsonPropertyName("firstName"), Description("The first name of the new user (required, max 100 chars)")] string FirstName,
    [property: JsonPropertyName("username"), Description("The unique login username (required, max 50 chars). If not given, derive it from the email or name.")] string Username,
    [property: JsonPropertyName("password"), Description("The login password (required, at least 6 characters)")] string Password,
    [property: JsonPropertyName("role"), Description("The system role. Allowed values: Patient, Doctor")] string Role,
    [property: JsonPropertyName("lastName"), Description("The last name of the new user (optional)")] string? LastName = null,
    [property: JsonPropertyName("title"), Description("The title, e.g. Dr., Mr., Ms. (optional)")] string? Title = null,
    [property: JsonPropertyName("email"), Description("The email address (optional)")] string? Email = null,
    [property: JsonPropertyName("age"), Description("The age in years, 0-120 (optional)")] int? Age = null,
    [property: JsonPropertyName("gender"), Description("The gender, e.g. Male, Female (optional)")] string? Gender = null,
    [property: JsonPropertyName("department"), Description("Department for doctors. Allowed values: NA, Medicine, Surgery, Dermatology, Pediatrics, Dentistry, Cardiology (optional)")] string? Department = null,
    [property: JsonPropertyName("hospitalIds"), Description("IDs of hospitals the user belongs to (optional)")] List<int>? HospitalIds = null
);

#endregion

[McpServerToolType]
public sealed class UserTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/user";

    [KernelFunction, Description(
        "Returns the signed-in user you are working for: id, name, username, email, age, gender, department and " +
        "userType (\"Doctor\" or \"Patient\"). Call this FIRST in every conversation to know who the user is and " +
        "which actions they may perform (Patients book appointments; Doctors manage schedules and approve/decline appointments).")]
    [McpServerTool]
    public async Task<JsonElement> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync($"{BasePath}/me", cancellationToken);
        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Returns a list of all registered system users.")]
    [McpServerTool]
    public async Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(BasePath, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: cancellationToken);
        return users ?? [];
    }

    [KernelFunction, Description(
        "Returns doctors that have upcoming schedules, with their profile (id, name, title, department) and each " +
        "schedule's scheduleId, date range, capacity and scheduleDays (scheduleDayId, date, startTime, endTime). " +
        "Use this to find which doctors work on which dates in one call. To see which exact time slots are still " +
        "free for booking, use the Appointments GetDoctorAvailability tool. Requires authorization.")]
    [McpServerTool]
    public async Task<JsonElement> GetDoctorsWithSchedule(
        [Description("Optional department filter. Allowed values: NA, Medicine, Surgery, Dermatology, Pediatrics, Dentistry, Cardiology. Leave empty for all doctors.")]
        string? department = null,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        var path = string.IsNullOrWhiteSpace(department)
            ? $"{BasePath}/doctors-with-schedule"
            : $"{BasePath}/doctors-with-schedule?department={Uri.EscapeDataString(department)}";

        using var response = await client.GetAsync(path, cancellationToken);
        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Returns a user by their unique ID.")]
    [McpServerTool]
    public async Task<UserDto?> GetUserById(
        [Description("The unique integer ID of the user to retrieve")] int id,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync($"{BasePath}/{id}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Creates a new user (Patient or Doctor). Requires first name, username, password (min 6 chars) and role; email, age, gender, last name, title, department and hospital IDs are optional.")]
    [McpServerTool]
    public async Task<UserDto?> CreateUser(
        [Description("The user details required to create the account")] CreateUserRequest user,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PostAsJsonAsync(BasePath, user, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Updates an existing user by their ID. Send the full user details (first name, username, password, role and any optional fields).")]
    [McpServerTool]
    public async Task<UserDto?> UpdateUser(
        [Description("The unique integer ID of the user to update")] int id,
        [Description("The full updated user details")] CreateUserRequest user,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PutAsJsonAsync($"{BasePath}/{id}", user, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Deletes a user permanently from the system using their ID.")]
    [McpServerTool]
    public async Task<string> DeleteUser(
        [Description("The unique integer ID of the user to delete")] int id,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.DeleteAsync($"{BasePath}/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return $"User with ID {id} deleted successfully.";
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
