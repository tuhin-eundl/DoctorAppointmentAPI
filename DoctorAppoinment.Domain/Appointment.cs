using System.ComponentModel.DataAnnotations;

namespace DoctorAppointment.Domain;

/// <summary>
/// A patient's requested time slot on one schedule day. A day is split into slots of the schedule's
/// appointment length, starting at the day's start time; SerialNumber is the slot's number (1-based).
/// Several patients can request the same slot, but a unique index on (day, serial) lets only one
/// of them be approved.
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
    /// <summary>
    /// The patient's reason for the visit. When the doctor declines or cancels, it's replaced
    /// by the doctor's explanation for the patient (or null if they gave none). When the request
    /// closes as SlotTaken, it's replaced by an automatic schedule-conflict message.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public enum AppointmentStatus
{
    /// <summary>Requested by the patient, waiting for the doctor.</summary>
    Pending,
    Approved,
    Declined,
    Cancelled,
    /// <summary>Closed automatically: the doctor approved another patient for the same time slot.</summary>
    SlotTaken
}
