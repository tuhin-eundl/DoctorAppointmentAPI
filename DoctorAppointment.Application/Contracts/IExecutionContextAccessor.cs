namespace DoctorAppointment.Application.Contracts;

public interface IExecutionContextAccessor
{
    string UserId { get; }
    string UserName { get; }
}

