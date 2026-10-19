using DoctorAppointment.Application.DTOs.Hospital;
using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Profiles.Users;

public static class UserProfiles
{
    public static User DtoToEntity(CreateUserDto userDto)
    {
        return new User
        {
            Id = userDto.Id,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Title = userDto.Title,
            Username = userDto.Username,
            Password = userDto.Password,
            Email = userDto.Email,
            Age = userDto.Age,
            Gender = userDto.Gender,
            Role = Enum.Parse<UserRole>(userDto.Role, ignoreCase: true),
            Department = userDto.Department is null
                ? Department.NA
                : Enum.Parse<Department>(userDto.Department, ignoreCase: true),
            Hospitals = userDto.HospitalIds is null
                ? new List<Hospital>()
                : userDto.HospitalIds.Select(id => new Hospital { Id = userDto.Id }).ToList()
        };
    }

    public static UserDto EntityToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Title,
            user.Username,
            null,
            user.Email,
            user.Age,
            user.Gender,
            user.Role.ToString(),
            user.Department.ToString(),
            user.Hospitals.Select(hospital => new HospitalDto(
                hospital.Id,
                hospital.Name,
                hospital.Address,
                hospital.Longitude,
                hospital.Latitude
            )).ToList()
        );

    }
}

