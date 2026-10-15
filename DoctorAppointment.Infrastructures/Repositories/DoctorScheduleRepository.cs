using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using DoctorAppointment.Infrastructures.Database;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures.Repositories;

public sealed class DoctorScheduleRepository : BaseRepository<DoctorSchedule>, IDoctorScheduleRepository
{
    private readonly AppDbContext _db;

    public DoctorScheduleRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DoctorSchedule>> GetAllWithDaysAsync()
    {
        return await _db.DoctorSchedules
            .Where(schedule => !schedule.IsDeleted && schedule.ScheduleDays.Any(day => !day.IsDeleted))
            .Include(schedule => schedule.ScheduleDays)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<DoctorSchedule>> GetByDoctorIdWithDaysAsync(int doctorId)
    {
        return await _db.DoctorSchedules
            .Where(schedule => !schedule.IsDeleted &&
                               schedule.DoctorId == doctorId &&
                               schedule.ScheduleDays.Any(day => !day.IsDeleted))
            .Include(schedule => schedule.ScheduleDays)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<DoctorSchedule?> GetWithDaysAsync(int scheduleId)
    {
        return _db.DoctorSchedules
            .Where(schedule => !schedule.IsDeleted && schedule.ScheduleDays.Any(day => !day.IsDeleted))
            .Include(schedule => schedule.ScheduleDays)
            .SingleOrDefaultAsync(schedule => schedule.Id == scheduleId);
    }

    public Task<bool> HasOverlappingScheduleAsync(int doctorId, DateOnly fromDate, DateOnly toDate, int? excludedScheduleId = null)
    {
        return _db.DoctorSchedules.AnyAsync(schedule =>
            schedule.DoctorId == doctorId &&
            schedule.FromDate <= toDate &&
            schedule.ToDate >= fromDate &&
            schedule.ScheduleDays.Any(day => !day.IsDeleted) &&
            (!excludedScheduleId.HasValue || schedule.Id != excludedScheduleId.Value));
    }

    public override async Task<bool> DeleteAsync(DoctorSchedule schedule)
    {
        schedule.IsDeleted = true;
        foreach (var day in schedule.ScheduleDays)
        {
            day.IsDeleted = true;
        }

        return await base.DeleteAsync(schedule);
    }
}

