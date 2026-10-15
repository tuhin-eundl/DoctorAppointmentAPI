namespace DoctorAppointment.Application.DTOs.DoctorSchedule;

public record DoctorScheduleDayDto(
    int DoctorScheduleId,
    DateOnly Date,
    TimeSpan StartTime,
    TimeSpan EndTime
);

