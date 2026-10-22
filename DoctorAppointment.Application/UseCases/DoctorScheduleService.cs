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
    private readonly IAppointmentRepository _appointmentRepository;

    public DoctorScheduleService(
        IDoctorScheduleRepository repository,
        IUserRepository userRepository,
        IAppointmentRepository appointmentRepository) : base(repository)
    {
        _repository = repository;
        _userRepository = userRepository;
        _appointmentRepository = appointmentRepository;
    }

    public async Task<DoctorSchedule> CreateScheduleAsync(int doctorId, DoctorScheduleDto request)
    {
        await EnsureDoctorAsync(doctorId);

        ValidateScheduleRequest(request.FromDate, request.ToDate, request.ScheduleDays);
        ValidateCapacity(request.MaxAppointmentsPerDay, request.AverageAppointmentMinutes);

        if (await _repository.HasOverlappingScheduleAsync(doctorId, request.FromDate, request.ToDate))
        {
            throw new ConflictException("A schedule already exists for one or more dates in this range. Delete it before adding a replacement.");
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = doctorId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            MaxAppointmentsPerDay = request.MaxAppointmentsPerDay,
            AverageAppointmentMinutes = request.AverageAppointmentMinutes,
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
        ValidateCapacity(request.MaxAppointmentsPerDay, request.AverageAppointmentMinutes);

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

        // Don't drop days, or shrink a day below its bookings, while patients hold places on it.
        var activeBookings = await _appointmentRepository.CountActiveByDayAsync(
            schedule.ScheduleDays.Select(day => day.Id).ToList());
        var removedWithBookings = schedule.ScheduleDays
            .Where(day => !requestedDays.ContainsKey(day.ScheduleDate) && activeBookings.GetValueOrDefault(day.Id) > 0)
            .Select(day => day.ScheduleDate.ToString("MMM d"))
            .ToList();
        if (removedWithBookings.Count > 0)
        {
            throw new ConflictException(
                $"These days already have appointments: {string.Join(", ", removedWithBookings)}. Cancel or decline them before removing the days.");
        }
        var overFull = schedule.ScheduleDays.FirstOrDefault(day =>
            activeBookings.GetValueOrDefault(day.Id) > request.MaxAppointmentsPerDay);
        if (overFull is not null)
        {
            throw new ConflictException(
                $"{overFull.ScheduleDate:MMM d} already has {activeBookings[overFull.Id]} appointments, more than the new limit of {request.MaxAppointmentsPerDay}.");
        }

        // Booked appointments hold fixed time slots, so the slot grid under them can't move.
        var booked = await _appointmentRepository.GetActiveOnDaysAsync(
            schedule.ScheduleDays.Select(day => day.Id).ToList());
        if (booked.Count > 0 && request.AverageAppointmentMinutes != schedule.AverageAppointmentMinutes)
        {
            throw new ConflictException(
                "Patients have already booked times in this schedule, so the appointment length can't change. Cancel or decline those appointments first.");
        }
        foreach (var day in schedule.ScheduleDays.Where(day => requestedDays.ContainsKey(day.ScheduleDate)))
        {
            var dayBookings = booked.Where(a => a.ScheduleDayId == day.Id).ToList();
            if (dayBookings.Count == 0) continue;

            var requested = requestedDays[day.ScheduleDate];
            if (requested.StartTime != day.StartTime)
            {
                throw new ConflictException(
                    $"{day.ScheduleDate:MMM d} has bookings, so its start time can't change.");
            }
            var lastBookedEnd = dayBookings.Max(a => a.StartTime).AddMinutes(schedule.AverageAppointmentMinutes);
            if (requested.EndTime < lastBookedEnd)
            {
                throw new ConflictException(
                    $"{day.ScheduleDate:MMM d} has a booking until {TimeSlots.Format(lastBookedEnd)}, so it can't end earlier than that.");
            }
        }

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
        schedule.MaxAppointmentsPerDay = request.MaxAppointmentsPerDay;
        schedule.AverageAppointmentMinutes = request.AverageAppointmentMinutes;

        return await _repository.UpdateAsync(schedule);
    }

    public async Task DeleteScheduleAsync(int doctorId, int scheduleId)
    {
        await EnsureDoctorAsync(doctorId);
        var schedule = await GetScheduleForDoctorAsync(doctorId, scheduleId);

        var activeBookings = await _appointmentRepository.CountActiveByDayAsync(
            schedule.ScheduleDays.Select(day => day.Id).ToList());
        if (activeBookings.Values.Any(count => count > 0))
        {
            throw new ConflictException(
                "This schedule has pending or approved appointments. Cancel or decline them before deleting it.");
        }

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

        var activeBookings = await _appointmentRepository.CountActiveByDayAsync([scheduleDayId]);
        if (activeBookings.GetValueOrDefault(scheduleDayId) > 0)
        {
            throw new ConflictException("This day has pending or approved appointments. Cancel or decline them first.");
        }

        // Remove only this day (this used to delete the whole schedule).
        scheduleDay.IsDeleted = true;
        await _repository.UpdateAsync(schedule);
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

    private static void ValidateCapacity(int maxAppointmentsPerDay, int averageAppointmentMinutes)
    {
        if (maxAppointmentsPerDay is < 1 or > 200)
        {
            throw new ValidationException("Appointments per day must be between 1 and 200.");
        }
        if (averageAppointmentMinutes is < 5 or > 240)
        {
            throw new ValidationException("The average appointment length must be between 5 and 240 minutes.");
        }
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

