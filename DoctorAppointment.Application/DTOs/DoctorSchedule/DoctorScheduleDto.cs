namespace DoctorAppointment.Application.DTOs.DoctorSchedule;

public record DoctorScheduleDto(
    int DoctorId,
    DateOnly FromDate,
    DateOnly ToDate,
    ICollection<DoctorScheduleDayDto> ScheduleDays
    );

