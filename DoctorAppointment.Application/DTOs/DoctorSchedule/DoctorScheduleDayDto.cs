namespace DoctorAppointment.Application.DTOs.DoctorSchedule;

public record DoctorScheduleDayDto(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? ScheduleDayId = null
);

