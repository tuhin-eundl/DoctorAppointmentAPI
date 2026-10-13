namespace DoctorAppointment.Application.Exceptions;

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException() : base("User already exists.")
    {
    }

    public UserAlreadyExistsException(string userName) : base($"User with username {userName} already exists.")
    {
    }

    public UserAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
