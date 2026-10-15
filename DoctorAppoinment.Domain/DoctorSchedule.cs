namespace DoctorAppointment.Domain;

public sealed class DoctorSchedule : AuditableEntity
{
    public int DoctorId { get; set; }
    public User? Doctor { get; set; } = null!;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public ICollection<DoctorScheduleDay> ScheduleDays { get; set; } = [];
}
