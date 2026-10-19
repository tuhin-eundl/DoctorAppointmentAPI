using System.ComponentModel.DataAnnotations;

namespace DoctorAppointment.Domain;

public sealed class User : AuditableEntity
{
    [MaxLength(100)]
    public required string FirstName { get; set; }
    [MaxLength(100)]
    public string? LastName { get; set; }
    [MaxLength(50)]
    public string? Title { get; set; }
    [MaxLength(50)]
    public required string Username { get; set; }
    [MinLength(6)]
    public required string Password { get; set; }
    public string? Email { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public UserRole Role { get; set; }
    public Department? Department { get; set; } = null;
    public ICollection<DoctorSchedule> DoctorSchedules { get; set; } = [];
    public ICollection<Hospital> Hospitals { get; set; } = [];
}

public enum UserRole
{
    Patient,
    Doctor
}

public enum Department
{
    NA,
    Medicine,
    Surgery,
    Dermatology,
    Pediatrics,
    Dentistry,
    Cardiology
}
