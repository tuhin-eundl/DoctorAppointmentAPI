using DoctorAppointment.Application.DTOs.Appointments;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Profiles.Appointments;

public static class AppointmentProfile
{
    public static AppointmentDto EntityToDto(Appointment appointment)
    {
        var schedule = appointment.ScheduleDay?.DoctorSchedule;

        return new AppointmentDto(
            appointment.Id,
            appointment.AppointmentDate,
            appointment.StartTime,
            schedule is null ? null : appointment.StartTime.AddMinutes(schedule.AverageAppointmentMinutes),
            appointment.SerialNumber,
            appointment.Status.ToString(),
            CancelledBy(appointment),
            appointment.Reason,
            appointment.CreatedAt,
            ToPerson(appointment.Doctor, appointment.DoctorId),
            ToPerson(appointment.Patient, appointment.PatientId));
    }

    private static string? CancelledBy(Appointment appointment)
    {
        if (appointment.Status != AppointmentStatus.Cancelled || appointment.CancelledById is null) return null;
        if (appointment.CancelledById == appointment.DoctorId) return "Doctor";
        return appointment.CancelledById == appointment.PatientId ? "Patient" : null;
    }

    private static AppointmentPersonDto ToPerson(User? user, int id) =>
        user is null
            ? new AppointmentPersonDto(id, "Unknown user", null, null, null, null)
            : new AppointmentPersonDto(
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Title,
                user.Department?.ToString(),
                user.Age,
                user.Gender);
}
