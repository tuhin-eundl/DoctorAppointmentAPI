namespace DoctorAppointment.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key) : base($"{name} with {key} not found!")
    {
    }
}
