using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.UseCases;

public sealed class HospitalService : BaseService<Hospital>, IHospitalService
{
    private readonly IHospitalRepository _repository;

    public HospitalService(IHospitalRepository repository) : base(repository)
    {
        _repository = repository;
    }
}

