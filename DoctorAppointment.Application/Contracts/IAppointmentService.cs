using DoctorAppointment.Application.DTOs.Appointments;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface IAppointmentService
{
    Task<IReadOnlyList<DoctorSummaryDto>> GetDoctorsAsync();
    Task<IReadOnlyList<AvailableDayDto>> GetAvailabilityAsync(int doctorId);
    Task<Appointment> BookAsync(int patientId, BookAppointmentDto request);
    /// <summary>A doctor's appointments, or a patient's own, depending on the user's role.</summary>
    Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId);
    /// <summary>
    /// Approving a request closes other patients' requests for the same slot. A doctor's reason for
    /// declining or cancelling replaces the patient's visit reason.
    /// </summary>
    Task<Appointment> UpdateStatusAsync(int userId, int appointmentId, string status, string? reason);
}
