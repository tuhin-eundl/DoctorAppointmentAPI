namespace DoctorAppointment.Application.DTOs.DoctorSchedule;

public record UpdateDoctorScheduleDto(
    DateOnly FromDate,
    DateOnly ToDate,
    ICollection<DoctorScheduleDayDto> ScheduleDays
);
