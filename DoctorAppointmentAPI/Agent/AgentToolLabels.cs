using Microsoft.SemanticKernel;

namespace DoctorAppointment.Service.Agent;

/// <summary>
/// How a tool call is described to the user while it runs and once it finishes.
/// <paramref name="Changes"/> names the data area a successful call modifies, so the app can refresh
/// screens showing it: appointments, schedules, notifications or users.
/// </summary>
public sealed record ToolProgress(string Running, string Done, string? Changes = null);

public static class AgentToolLabels
{
    private static readonly ToolProgress Fallback = new("Working on it", "Step finished");

    // Keyed by KernelFunction name; the names are unique across all plugins.
    private static readonly Dictionary<string, ToolProgress> Labels = new()
    {
        // UserManagement
        ["GetCurrentUser"] = new("Checking your account", "Account checked"),
        ["GetAllUsers"] = new("Looking up users", "Found the users"),
        ["GetDoctorsWithSchedule"] = new("Finding doctors and their working days", "Found the doctors' working days"),
        ["GetUserById"] = new("Looking up the user", "Found the user"),
        ["CreateUser"] = new("Creating the account", "Account created", "users"),
        ["UpdateUser"] = new("Updating the account", "Account updated", "users"),
        ["DeleteUser"] = new("Deactivating the account", "Account deactivated", "users"),

        // Appointments
        ["GetBookableDoctors"] = new("Finding available doctors", "Found available doctors"),
        ["GetDoctorAvailability"] = new("Checking open time slots", "Checked open time slots"),
        ["BookAppointment"] = new("Booking your appointment", "Appointment booked", "appointments"),
        ["GetMyAppointments"] = new("Fetching your appointments", "Fetched your appointments"),
        ["UpdateAppointmentStatus"] = new("Updating the appointment", "Appointment updated", "appointments"),

        // DoctorSchedules
        ["GetMySchedules"] = new("Reviewing your schedules", "Reviewed your schedules"),
        ["GetAllSchedules"] = new("Looking up schedules", "Found the schedules"),
        ["GetSchedulesByDoctorId"] = new("Looking up the doctor's schedules", "Found the doctor's schedules"),
        ["GetScheduleById"] = new("Opening the schedule", "Opened the schedule"),
        ["CreateSchedule"] = new("Creating your schedule", "Schedule created", "schedules"),
        ["UpdateSchedule"] = new("Updating your schedule", "Schedule updated", "schedules"),
        ["DeleteSchedule"] = new("Deleting the schedule", "Schedule deleted", "schedules"),
        ["DeleteScheduleDay"] = new("Removing the day from your schedule", "Day removed from your schedule", "schedules"),

        // Notifications
        ["GetMyNotifications"] = new("Checking your notifications", "Checked your notifications"),
        ["MarkNotificationAsRead"] = new("Marking the notification as read", "Notification marked as read", "notifications"),

        // Hospitals
        ["GetHospitals"] = new("Looking up hospitals", "Found the hospitals"),
    };

    public static ToolProgress For(string functionName, KernelArguments? arguments)
    {
        // Name the actual status change rather than a generic "updating".
        if (functionName == "UpdateAppointmentStatus"
            && arguments?.TryGetValue("status", out var status) == true)
        {
            switch (status?.ToString()?.Trim().ToLowerInvariant())
            {
                case "approved":
                    return new("Approving the appointment", "Appointment approved", "appointments");
                case "declined":
                    return new("Declining the appointment", "Appointment declined", "appointments");
                case "cancelled":
                    return new("Cancelling the appointment", "Appointment cancelled", "appointments");
            }
        }

        return Labels.GetValueOrDefault(functionName, Fallback);
    }
}
