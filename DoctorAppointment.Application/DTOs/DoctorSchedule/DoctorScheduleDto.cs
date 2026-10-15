namespace DoctorAppointment.Application.DTOs.DoctorSchedule;

public record DoctorScheduleDto(
    DateOnly FromDate,
    DateOnly ToDate,
    ICollection<DoctorScheduleDayDto> ScheduleDays,
    int? ScheduleId = null
);

