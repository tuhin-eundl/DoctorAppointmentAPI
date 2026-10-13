namespace DoctorAppointment.Application.Exceptions;

public class DatabaseConfigurationException : Exception
{
    public DatabaseConfigurationException()
        : base("Database configuration is missing or not compatible.")
    {
    }

    public DatabaseConfigurationException(string message)
        : base(message)
    {
    }

    public DatabaseConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
