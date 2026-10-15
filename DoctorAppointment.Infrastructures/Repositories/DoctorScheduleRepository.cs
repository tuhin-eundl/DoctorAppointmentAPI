using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using DoctorAppointment.Infrastructures.Database;

namespace DoctorAppointment.Infrastructures.Repositories;

public sealed class DoctorScheduleRepository : BaseRepository<DoctorSchedule>, IDoctorScheduleRepository
{
    AppDbContext _db;

    public DoctorScheduleRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }
}

