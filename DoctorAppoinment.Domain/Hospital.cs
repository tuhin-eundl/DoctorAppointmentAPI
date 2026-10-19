namespace DoctorAppointment.Domain;

public sealed class Hospital : AuditableEntity
{
    public string Name { get; set; }
    public string? Address { get; set; }
    public string? Longitude { get; set; }
    public string? Latitude { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}

