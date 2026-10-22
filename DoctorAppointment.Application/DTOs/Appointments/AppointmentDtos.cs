namespace DoctorAppointment.Application.DTOs.Appointments;

/// <summary>A patient asks for a time slot on one of a doctor's schedule days.</summary>
public record BookAppointmentDto(int ScheduleDayId, TimeOnly StartTime, string? Reason);

/// <summary>Approved, Declined (doctor) or Cancelled (patient or doctor).</summary>
public record UpdateAppointmentStatusDto(string Status);

public record AppointmentPersonDto(
    int Id,
    string Name,
    string? Title,
    string? Department,
    int? Age,
    string? Gender);

public record AppointmentDto(
    int Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly? EndTime, // StartTime plus the appointment length; null if the schedule was removed
    int SerialNumber,
    string Status,
    string? CancelledBy, // "Doctor" or "Patient" when cancelled, otherwise null
    string? Reason,
    DateTime RequestedAt,
    AppointmentPersonDto Doctor,
    AppointmentPersonDto Patient);

public record DoctorSummaryDto(
    int Id,
    string FirstName,
    string? LastName,
    string? Title,
    string? Department,
    DateOnly? NextAvailableDate);

/// <summary>
/// One time slot. State is "Free", "Booked" (another patient holds it) or
/// "Unavailable" (already passed today, or the day reached its patient limit).
/// </summary>
public record TimeSlotDto(TimeOnly Time, string State);

/// <summary>One bookable day of a doctor's schedule, split into time slots.</summary>
public record AvailableDayDto(
    int ScheduleDayId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int AverageAppointmentMinutes,
    int Capacity,
    int Booked,
    int FreeSlots,
    IReadOnlyList<TimeSlotDto> Slots);
