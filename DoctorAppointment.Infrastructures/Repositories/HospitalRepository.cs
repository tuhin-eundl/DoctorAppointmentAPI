using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using DoctorAppointment.Infrastructures.Database;

namespace DoctorAppointment.Infrastructures.Repositories;

public sealed class HospitalRepository : BaseRepository<Hospital>, IHospitalRepository
{
    AppDbContext _db;

    public HospitalRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }
}

