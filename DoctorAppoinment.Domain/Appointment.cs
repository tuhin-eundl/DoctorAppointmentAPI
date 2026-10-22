using System.ComponentModel.DataAnnotations;

namespace DoctorAppointment.Domain;

/// <summary>
/// A patient's booked time slot on one schedule day. A day is split into slots of the schedule's
/// appointment length, starting at the day's start time; SerialNumber is the slot's number (1-based),
/// so a unique index on (day, serial) stops two active appointments sharing a slot.
/// </summary>
public sealed class Appointment : AuditableEntity
{
    public int PatientId { get; set; }
    public User? Patient { get; set; }
    public int DoctorId { get; set; }
    public User? Doctor { get; set; }
    public int ScheduleDayId { get; set; }
    public DoctorScheduleDay? ScheduleDay { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public int SerialNumber { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    /// <summary>The user who cancelled it: the doctor or the patient.</summary>
    public int? CancelledById { get; set; }
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public enum AppointmentStatus
{
    /// <summary>Requested by the patient, waiting for the doctor.</summary>
    Pending,
    Approved,
    Declined,
    Cancelled
}
