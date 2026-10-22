using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface IAppointmentRepository : IBaseRepository<Appointment>
{
    /// <summary>
    /// Loads the appointment with patient, doctor and schedule day (including soft-deleted ones), untracked.
    /// To change an appointment, load it with GetAsync instead.
    /// </summary>
    Task<Appointment?> GetWithDetailsAsync(int appointmentId);
    Task<IReadOnlyList<Appointment>> GetForPatientAsync(int patientId);
    Task<IReadOnlyList<Appointment>> GetForDoctorAsync(int doctorId);
    /// <summary>Pending or Approved appointments on the given schedule days, untracked.</summary>
    Task<List<Appointment>> GetActiveOnDaysAsync(IReadOnlyCollection<int> scheduleDayIds);
    /// <summary>Number of Pending or Approved appointments per schedule day.</summary>
    Task<Dictionary<int, int>> CountActiveByDayAsync(IReadOnlyCollection<int> scheduleDayIds);
    Task<bool> PatientHasActiveAppointmentAsync(int patientId, int doctorId, DateOnly date);
}
