using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using DoctorAppointment.Infrastructures.Database;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures.Repositories;

public sealed class AppointmentRepository : BaseRepository<Appointment>, IAppointmentRepository
{
    private static readonly AppointmentStatus[] ActiveStatuses = [AppointmentStatus.Pending, AppointmentStatus.Approved];

    private readonly AppDbContext _db;

    public AppointmentRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    // Query filters are ignored so an appointment still loads after its doctor, patient or
    // schedule day is soft-deleted; only the appointment's own IsDeleted flag is respected.
    // Read-only: updating a loaded graph would also rewrite the related users and schedule rows.
    private IQueryable<Appointment> WithDetails() =>
        _db.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.ScheduleDay)
                .ThenInclude(day => day!.DoctorSchedule);

    public Task<Appointment?> GetWithDetailsAsync(int appointmentId) =>
        WithDetails().SingleOrDefaultAsync(a => a.Id == appointmentId);

    public async Task<IReadOnlyList<Appointment>> GetForPatientAsync(int patientId) =>
        await WithDetails()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenBy(a => a.SerialNumber)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IReadOnlyList<Appointment>> GetForDoctorAsync(int doctorId) =>
        await WithDetails()
            .Where(a => a.DoctorId == doctorId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenBy(a => a.SerialNumber)
            .AsNoTracking()
            .ToListAsync();

    public Task<List<Appointment>> GetActiveOnDaysAsync(IReadOnlyCollection<int> scheduleDayIds) =>
        _db.Appointments
            .Where(a => scheduleDayIds.Contains(a.ScheduleDayId) && ActiveStatuses.Contains(a.Status))
            .AsNoTracking()
            .ToListAsync();

    public Task<List<Appointment>> GetApprovedOnDaysAsync(IReadOnlyCollection<int> scheduleDayIds) =>
        _db.Appointments
            .Where(a => scheduleDayIds.Contains(a.ScheduleDayId) && a.Status == AppointmentStatus.Approved)
            .AsNoTracking()
            .ToListAsync();

    public Task<List<Appointment>> GetPendingInSlotAsync(int scheduleDayId, int serialNumber, int exceptId) =>
        _db.Appointments
            .Where(a =>
                a.ScheduleDayId == scheduleDayId &&
                a.SerialNumber == serialNumber &&
                a.Id != exceptId &&
                a.Status == AppointmentStatus.Pending)
            .ToListAsync();

    public Task<Dictionary<int, int>> CountActiveByDayAsync(IReadOnlyCollection<int> scheduleDayIds) =>
        _db.Appointments
            .Where(a => scheduleDayIds.Contains(a.ScheduleDayId) && ActiveStatuses.Contains(a.Status))
            .GroupBy(a => a.ScheduleDayId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

    public Task<Appointment?> GetPatientActiveAppointmentAtAsync(int patientId, DateOnly date, TimeOnly startTime) =>
        WithDetails().FirstOrDefaultAsync(a =>
            a.PatientId == patientId &&
            a.AppointmentDate == date &&
            a.StartTime == startTime &&
            ActiveStatuses.Contains(a.Status));
}
