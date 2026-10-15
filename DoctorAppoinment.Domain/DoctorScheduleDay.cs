namespace DoctorAppointment.Domain;

public sealed class DoctorScheduleDay : AuditableEntity
{
    public int DoctorScheduleId { get; set; }
    public DoctorSchedule? DoctorSchedule { get; set; } = null!;
    public DateOnly ScheduleDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
