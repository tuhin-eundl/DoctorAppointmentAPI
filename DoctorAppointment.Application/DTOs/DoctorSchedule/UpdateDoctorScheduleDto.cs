namespace DoctorAppointment.Application.DTOs.DoctorSchedule;

public record UpdateDoctorScheduleDto(
    DateOnly FromDate,
    DateOnly ToDate,
    ICollection<DoctorScheduleDayDto> ScheduleDays,
    int MaxAppointmentsPerDay = 20,
    int AverageAppointmentMinutes = 15
);
