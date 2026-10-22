namespace DoctorAppointment.Domain;

public sealed class DoctorSchedule : AuditableEntity
{
    public int DoctorId { get; set; }
    public User? Doctor { get; set; } = null!;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    /// <summary>How many patients can book each day of this schedule.</summary>
    public int MaxAppointmentsPerDay { get; set; } = 20;
    /// <summary>Rough length of one appointment, used to estimate each patient's time.</summary>
    public int AverageAppointmentMinutes { get; set; } = 15;
    public ICollection<DoctorScheduleDay> ScheduleDays { get; set; } = [];
}
