namespace DoctorAppointment.Application.DTOs.Hospital;

    public record HospitalDto(
        int? Id,
        string Name,
        string? Address,
        string? Longitude,
        string? Latitude
    );

