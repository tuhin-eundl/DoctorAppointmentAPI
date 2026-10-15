using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface IDoctorScheduleRepository : IBaseRepository<DoctorSchedule>
{
    Task<IReadOnlyList<DoctorSchedule>> GetAllWithDaysAsync();
    Task<IReadOnlyList<DoctorSchedule>> GetByDoctorIdWithDaysAsync(int doctorId);
    Task<DoctorSchedule?> GetWithDaysAsync(int scheduleId);
    Task<bool> HasOverlappingScheduleAsync(int doctorId, DateOnly fromDate, DateOnly toDate, int? excludedScheduleId = null);
}

