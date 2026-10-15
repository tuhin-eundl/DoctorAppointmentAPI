using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.UseCases;

public class DoctorScheduleService : BaseService<DoctorSchedule>, IDoctorScheduleService, IService<DoctorSchedule>
{
    private readonly IDoctorScheduleRepository _repository;

    public DoctorScheduleService(IDoctorScheduleRepository repository) : base(repository)
    {
        _repository = repository;
    }
}

