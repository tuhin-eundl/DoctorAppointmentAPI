using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Validations.Users;

public static class UserDtoValidation
{
    public static IReadOnlyList<string> IsValid(UserDto userDto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(userDto.FirstName) || userDto.FirstName.Length > 100)
            errors.Add("First name is invalid.");
        if (!string.IsNullOrWhiteSpace(userDto.LastName) && userDto.LastName.Length > 100)
            errors.Add("Last name is invalid.");
        if (!string.IsNullOrWhiteSpace(userDto.Title) && userDto.Title.Length > 50)
            errors.Add("Title is invalid.");
        if (string.IsNullOrWhiteSpace(userDto.Username) || userDto.Username.Length > 50)
            errors.Add("Username is invalid.");
        if (string.IsNullOrWhiteSpace(userDto.Password))
            errors.Add("Password is invalid.");
        if (userDto.Password.Length < 6)
            errors.Add("Password must be at least 6 characters long.");
        if (userDto.Age.HasValue && (userDto.Age < 0 || userDto.Age > 120))
            errors.Add("Age is invalid.");
        if (!string.IsNullOrWhiteSpace(userDto.Role) && userDto.Role.Length > 50 && !Enum.IsDefined(typeof(UserRole), userDto.Role))
            errors.Add("Role is invalid.");
        if (!string.IsNullOrWhiteSpace(userDto.Department) && userDto.Department.Length > 100 && !Enum.IsDefined(typeof(Department), userDto.Department))
            errors.Add("Department is invalid.");
        return errors;
    }
}

