using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Profiles.Users;

public static class UserProfiles
{
    public static User DtoToEntity(UserDto userDto)
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
                : Enum.Parse<Department>(userDto.Department, ignoreCase: true)
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
            user.Department.ToString()
        );

    }
}

