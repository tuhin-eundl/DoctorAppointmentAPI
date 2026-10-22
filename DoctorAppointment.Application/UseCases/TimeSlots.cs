using DoctorAppointment.Domain;
using System.Globalization;

namespace DoctorAppointment.Application.UseCases;

/// <summary>
/// A schedule day is split into back-to-back slots of the appointment length, starting at the
/// day's start time. Slot numbers are 1-based; the last slot must end by the day's end time.
/// </summary>
public static class TimeSlots
{
    public static int Count(DoctorScheduleDay day, int length) =>
        day.EndTime <= day.StartTime ? 0 : (int)(day.EndTime - day.StartTime).TotalMinutes / length;

    public static TimeOnly StartOf(DoctorScheduleDay day, int length, int slot) =>
        day.StartTime.AddMinutes((slot - 1) * length);

    /// <summary>The slot that starts exactly at `time`, or null if no slot starts then.</summary>
    public static int? NumberAt(DoctorScheduleDay day, int length, TimeOnly time)
    {
        // TimeOnly subtraction wraps around midnight, so rule out earlier times first.
        if (time < day.StartTime) return null;
        var offset = (int)(time - day.StartTime).TotalMinutes;
        if (offset % length != 0) return null;
        var slot = offset / length + 1;
        return slot <= Count(day, length) ? slot : null;
    }

    /// <summary>True when the slot has already started (only possible today).</summary>
    public static bool HasStarted(DoctorScheduleDay day, int length, int slot, DateTime now) =>
        day.ScheduleDate < DateOnly.FromDateTime(now) ||
        (day.ScheduleDate == DateOnly.FromDateTime(now) && StartOf(day, length, slot) <= TimeOnly.FromDateTime(now));

    /// <summary>"9:30 AM"</summary>
    public static string Format(TimeOnly time) => time.ToString("h:mm tt", CultureInfo.InvariantCulture);
}
