using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.DoctorSchedule;
using DoctorAppointment.Application.Exceptions;
using DoctorAppointment.Domain;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Application.UseCases;

public sealed class DoctorScheduleService : BaseService<DoctorSchedule>, IDoctorScheduleService
{
    private readonly IDoctorScheduleRepository _repository;
    private readonly IUserRepository _userRepository;

    public DoctorScheduleService(
        IDoctorScheduleRepository repository,
        IUserRepository userRepository) : base(repository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public async Task<DoctorSchedule> CreateScheduleAsync(int doctorId, DoctorScheduleDto request)
    {
        await EnsureDoctorAsync(doctorId);

        ValidateScheduleRequest(request.FromDate, request.ToDate, request.ScheduleDays);

        if (await _repository.HasOverlappingScheduleAsync(doctorId, request.FromDate, request.ToDate))
        {
            throw new ConflictException("A schedule already exists for one or more dates in this range. Delete it before adding a replacement.");
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = doctorId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            ScheduleDays = request.ScheduleDays.Select(day => new DoctorScheduleDay
            {
                ScheduleDate = day.Date,
                StartTime = day.StartTime,
                EndTime = day.EndTime
            }).ToList()
        };

        return await _repository.AddAsync(schedule);
    }

    public Task<IReadOnlyList<DoctorSchedule>> GetAllSchedulesAsync()
    {
        return _repository.GetAllWithDaysAsync();
    }

    public async Task<IReadOnlyList<DoctorSchedule?>> GetSchedulesByDoctorIdAsync(int doctorId)
    {
        return await _repository.GetByDoctorIdWithDaysAsync(doctorId);
    }

    public async Task<DoctorSchedule?> GetScheduleByIdAsync(
        int scheduleId)
    {
        return await _repository.Query()
            .AsNoTracking()
            .Include(s => s.ScheduleDays)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
    }

    public async Task<DoctorSchedule> UpdateScheduleAsync(int scheduleId, UpdateDoctorScheduleDto request)
    {
        ValidateScheduleRequest(request.FromDate, request.ToDate, request.ScheduleDays);

        var schedule = await GetScheduleByIdAsync(scheduleId);
        if(schedule == null)
        {
            throw new NotFoundException(nameof(DoctorSchedule), scheduleId);
        }
        if (await _repository.HasOverlappingScheduleAsync(schedule.DoctorId, request.FromDate, request.ToDate, scheduleId))
        {
            throw new ConflictException("A schedule already exists for one or more dates in this range.");
        }

        var requestedDays = request.ScheduleDays.ToDictionary(day => day.Date);
        var existingDates = new HashSet<DateOnly>();

        foreach (var existingDay in schedule.ScheduleDays)
        {
            existingDates.Add(existingDay.ScheduleDate);
            if (requestedDays.TryGetValue(existingDay.ScheduleDate, out var requestedDay))
            {
                existingDay.StartTime = requestedDay.StartTime;
                existingDay.EndTime = requestedDay.EndTime;
            }
            else
            {
                existingDay.IsDeleted = true;
            }
        }

        foreach (var requestedDay in request.ScheduleDays.Where(day => !existingDates.Contains(day.Date)))
        {
            schedule.ScheduleDays.Add(new DoctorScheduleDay
            {
                ScheduleDate = requestedDay.Date,
                StartTime = requestedDay.StartTime,
                EndTime = requestedDay.EndTime
            });
        }

        schedule.FromDate = request.FromDate;
        schedule.ToDate = request.ToDate;

        return await _repository.UpdateAsync(schedule);
    }

    public async Task DeleteScheduleAsync(int doctorId, int scheduleId)
    {
        await EnsureDoctorAsync(doctorId);
        var schedule = await GetScheduleForDoctorAsync(doctorId, scheduleId);

        await _repository.DeleteAsync(schedule);
    }

    public async Task DeleteScheduleDayAsync(int doctorId, int scheduleId, int scheduleDayId)
    {
        await EnsureDoctorAsync(doctorId);
        var schedule = await GetScheduleForDoctorAsync(doctorId, scheduleId);
        if (schedule is null)
        {
            throw new NotFoundException(nameof(DoctorSchedule), scheduleId);
        }
        var scheduleDay = schedule.ScheduleDays.SingleOrDefault(day => day.Id == scheduleDayId);

        if (scheduleDay is null)
        {
            throw new NotFoundException(nameof(DoctorScheduleDay), scheduleDayId);
        }

        await _repository.DeleteAsync(schedule);
    }

    private async Task EnsureDoctorAsync(int doctorId)
    {
        var doctor = await _userRepository.GetAsync(doctorId);
        if (doctor is null || doctor.Role != UserRole.Doctor)
        {
            throw new ForbiddenException("Only users with the Doctor role can manage schedules.");
        }
    }

    private async Task<DoctorSchedule> GetScheduleForDoctorAsync(int doctorId, int scheduleId)
    {
        var schedule = await _repository.GetWithDaysAsync(scheduleId);
        if (schedule is null || schedule.DoctorId != doctorId)
        {
            throw new NotFoundException(nameof(DoctorSchedule), scheduleId);
        }

        return schedule;
    }

    private static void ValidateScheduleRequest(DateOnly fromDate, DateOnly toDate, ICollection<DoctorScheduleDayDto> scheduleDays)
    {
        if (fromDate > toDate)
        {
            throw new ValidationException("FromDate must be on or before ToDate.");
        }

        var expectedDayCount = toDate.DayNumber - fromDate.DayNumber + 1;
        if (scheduleDays is null ||
            scheduleDays.Count != expectedDayCount ||
            scheduleDays.Any(day => day is null || day.Date < fromDate || day.Date > toDate) ||
            scheduleDays.Select(day => day.Date).Distinct().Count() != expectedDayCount)
        {
            throw new ValidationException("Provide exactly one schedule entry within the date range for every date.");
        }

        if (scheduleDays.Any(day => day.StartTime > day.EndTime))
        {
            throw new ValidationException("StartTime cannot be later than EndTime.");
        }
    }
}

