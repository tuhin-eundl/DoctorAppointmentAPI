using DoctorAppointment.Application.DTOs.DoctorSchedule;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface IDoctorScheduleService : IService<DoctorSchedule>
{
    Task<DoctorSchedule> CreateScheduleAsync(int doctorId, DoctorScheduleDto request);
    Task<IReadOnlyList<DoctorSchedule>> GetAllSchedulesAsync();
    Task<IReadOnlyList<DoctorSchedule>> GetSchedulesByDoctorIdAsync(int authenticatedDoctorId, int doctorId);
    Task<DoctorSchedule> GetScheduleByIdAsync(int doctorId, int scheduleId);
    Task<DoctorSchedule> UpdateScheduleAsync(int doctorId, int scheduleId, UpdateDoctorScheduleDto request);
    Task DeleteScheduleDayAsync(int doctorId, int scheduleId, int scheduleDayId);
    Task DeleteScheduleAsync(int doctorId, int scheduleId);
}
