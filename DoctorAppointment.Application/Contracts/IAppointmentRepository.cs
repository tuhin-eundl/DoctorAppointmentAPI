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
    /// <summary>Approved appointments on the given schedule days, untracked. These are the slots that are taken.</summary>
    Task<List<Appointment>> GetApprovedOnDaysAsync(IReadOnlyCollection<int> scheduleDayIds);
    /// <summary>Other patients' pending requests for the same slot, tracked so they can be closed.</summary>
    Task<List<Appointment>> GetPendingInSlotAsync(int scheduleDayId, int serialNumber, int exceptId);
    /// <summary>Number of Pending or Approved appointments per schedule day.</summary>
    Task<Dictionary<int, int>> CountActiveByDayAsync(IReadOnlyCollection<int> scheduleDayIds);
    /// <summary>Marks Pending or Approved appointments whose time slot has ended as Overdue. Returns how many changed.</summary>
    Task<int> MarkOverdueAsync(DateTime now);
    Task<bool> PatientHasActiveAppointmentAsync(int patientId, int doctorId, DateOnly date, TimeOnly startTime);
}
