using DoctorAppointment.Application.DTOs.DoctorSchedule;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Profiles.DoctorSchedules;

public static class DoctorScheduleProfile
{
    public static DoctorScheduleDto EntityToDto(DoctorSchedule schedule)
    {
        return new DoctorScheduleDto(
            schedule.FromDate,
            schedule.ToDate,
            schedule.ScheduleDays
                .Where(day => !day.IsDeleted)
                .Select(day => new DoctorScheduleDayDto(day.ScheduleDate, day.StartTime, day.EndTime, day.Id))
                .ToList(),
            schedule.Id,
            schedule.MaxAppointmentsPerDay,
            schedule.AverageAppointmentMinutes);
    }
}
