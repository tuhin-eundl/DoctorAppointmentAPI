using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using DoctorAppointment.Infrastructures.Database;

namespace DoctorAppointment.Infrastructures.Repositories;

public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    AppDbContext _db;
    public UserRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }
}

