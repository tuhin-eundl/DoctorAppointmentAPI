using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Appointments;
using DoctorAppointment.Application.Exceptions;
using DoctorAppointment.Domain;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointment.Application.UseCases;

public sealed class AppointmentService : IAppointmentService
{
    private const string Free = "Free";
    private const string Booked = "Booked";
    private const string Unavailable = "Unavailable";

    private readonly IAppointmentRepository _repository;
    private readonly IDoctorScheduleRepository _scheduleRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;

    public AppointmentService(
        IAppointmentRepository repository,
        IDoctorScheduleRepository scheduleRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository)
    {
        _repository = repository;
        _scheduleRepository = scheduleRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
    }

    private sealed record BookableDay(DoctorScheduleDay Day, int DoctorId, int Capacity, int SlotMinutes);

    /// <summary>
    /// Schedule days that haven't ended yet (today counts until its end time), earliest first.
    /// All filters are applied before projecting, since EF can't translate filters on the projection.
    /// </summary>
    private async Task<List<BookableDay>> UpcomingDaysAsync(int? doctorId = null, int? scheduleDayId = null)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var timeNow = TimeOnly.FromDateTime(now);

        var days = await _scheduleRepository.Query()
            .Where(schedule => !schedule.IsDeleted && (doctorId == null || schedule.DoctorId == doctorId))
            .SelectMany(
                schedule => schedule.ScheduleDays.Where(day =>
                    !day.IsDeleted &&
                    (scheduleDayId == null || day.Id == scheduleDayId) &&
                    (day.ScheduleDate > today || (day.ScheduleDate == today && day.EndTime > timeNow))),
                (schedule, day) => new BookableDay(day, schedule.DoctorId, schedule.MaxAppointmentsPerDay, schedule.AverageAppointmentMinutes))
            .AsNoTracking()
            .ToListAsync();

        return days.OrderBy(x => x.Day.ScheduleDate).ToList();
    }

    /// <summary>Every slot of the day with its state, given the slot numbers already taken.</summary>
    private static List<TimeSlotDto> SlotsFor(BookableDay x, ICollection<int> taken, DateTime now)
    {
        var full = taken.Count >= x.Capacity;
        return Enumerable.Range(1, TimeSlots.Count(x.Day, x.SlotMinutes))
            .Select(slot => new TimeSlotDto(
                TimeSlots.StartOf(x.Day, x.SlotMinutes, slot),
                taken.Contains(slot) ? Booked
                    : full || TimeSlots.HasStarted(x.Day, x.SlotMinutes, slot, now) ? Unavailable
                    : Free))
            .ToList();
    }

    private async Task<Dictionary<int, HashSet<int>>> TakenSlotsAsync(IEnumerable<BookableDay> days)
    {
        var active = await _repository.GetActiveOnDaysAsync(days.Select(x => x.Day.Id).ToList());
        return active
            .GroupBy(a => a.ScheduleDayId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.SerialNumber).ToHashSet());
    }

    public async Task<IReadOnlyList<DoctorSummaryDto>> GetDoctorsAsync()
    {
        var doctors = await _userRepository.Query()
            .Where(user => user.Role == UserRole.Doctor)
            .OrderBy(user => user.FirstName)
            .AsNoTracking()
            .ToListAsync();

        var now = DateTime.Now;
        var days = await UpcomingDaysAsync();
        var taken = await TakenSlotsAsync(days);

        var nextAvailable = days
            .Where(x => SlotsFor(x, taken.GetValueOrDefault(x.Day.Id) ?? [], now).Any(s => s.State == Free))
            .GroupBy(x => x.DoctorId)
            .ToDictionary(g => g.Key, g => g.Min(x => x.Day.ScheduleDate));

        return doctors
            .Select(doctor => new DoctorSummaryDto(
                doctor.Id,
                doctor.FirstName,
                doctor.LastName,
                doctor.Title,
                doctor.Department?.ToString(),
                nextAvailable.TryGetValue(doctor.Id, out var date) ? date : null))
            .ToList();
    }

    public async Task<IReadOnlyList<AvailableDayDto>> GetAvailabilityAsync(int doctorId)
    {
        var now = DateTime.Now;
        var days = await UpcomingDaysAsync(doctorId: doctorId);
        var taken = await TakenSlotsAsync(days);

        return days
            .Select(x =>
            {
                var dayTaken = taken.GetValueOrDefault(x.Day.Id) ?? [];
                var slots = SlotsFor(x, dayTaken, now);
                return new AvailableDayDto(
                    x.Day.Id,
                    x.Day.ScheduleDate,
                    x.Day.StartTime,
                    x.Day.EndTime,
                    x.SlotMinutes,
                    x.Capacity,
                    dayTaken.Count,
                    slots.Count(s => s.State == Free),
                    slots);
            })
            .ToList();
    }

    /// <summary>"Free times nearby: 10:30 AM, 11:00 AM and 11:15 AM." — the free slots closest to the requested one.</summary>
    private static string NearbyFreeTimes(List<TimeSlotDto> slots, TimeOnly requested)
    {
        var nearest = slots
            .Where(s => s.State == Free)
            .OrderBy(s => Math.Abs((s.Time.ToTimeSpan() - requested.ToTimeSpan()).TotalMinutes))
            .Take(3)
            .OrderBy(s => s.Time)
            .Select(s => TimeSlots.Format(s.Time))
            .ToList();

        return nearest.Count switch
        {
            0 => "There are no other free times that day. Please pick another day.",
            1 => $"The nearest free time is {nearest[0]}.",
            _ => $"Free times nearby: {string.Join(", ", nearest[..^1])} and {nearest[^1]}."
        };
    }

    public async Task<Appointment> BookAsync(int patientId, BookAppointmentDto request)
    {
        var patient = await _userRepository.GetAsync(patientId);
        if (patient is null || patient.Role != UserRole.Patient)
        {
            throw new ForbiddenException("Only patients can book appointments.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason?.Length > 500)
        {
            throw new ValidationException("Keep the reason to 500 characters or fewer.");
        }

        var target = (await UpcomingDaysAsync(scheduleDayId: request.ScheduleDayId)).SingleOrDefault()
            ?? throw new ValidationException("That day is no longer available. Please pick another day.");

        var length = target.SlotMinutes;
        var slotCount = TimeSlots.Count(target.Day, length);
        var slot = TimeSlots.NumberAt(target.Day, length, request.StartTime);
        if (slot is null)
        {
            var last = TimeSlots.StartOf(target.Day, length, Math.Max(slotCount, 1));
            throw new ValidationException(
                $"Pick a time between {TimeSlots.Format(target.Day.StartTime)} and {TimeSlots.Format(last)}, in {length}-minute steps.");
        }

        var now = DateTime.Now;
        if (TimeSlots.HasStarted(target.Day, length, slot.Value, now))
        {
            throw new ValidationException("That time has already passed. Please pick a later time.");
        }

        if (await _repository.PatientHasActiveAppointmentAsync(patientId, target.DoctorId, target.Day.ScheduleDate))
        {
            throw new ConflictException("You already have an appointment with this doctor on that day.");
        }

        var taken = (await TakenSlotsAsync([target])).GetValueOrDefault(target.Day.Id) ?? [];
        var slots = SlotsFor(target, taken, now);
        if (taken.Count >= target.Capacity)
        {
            throw new ConflictException("That day is fully booked. Please pick another day.");
        }
        if (taken.Contains(slot.Value))
        {
            throw new ConflictException(
                $"{TimeSlots.Format(request.StartTime)} is already booked. {NearbyFreeTimes(slots, request.StartTime)}");
        }

        Appointment booked;
        try
        {
            booked = await _repository.AddAsync(new Appointment
            {
                PatientId = patientId,
                DoctorId = target.DoctorId,
                ScheduleDayId = target.Day.Id,
                AppointmentDate = target.Day.ScheduleDate,
                StartTime = request.StartTime,
                SerialNumber = slot.Value,
                Status = AppointmentStatus.Pending,
                Reason = reason
            });
        }
        catch (DbUpdateException ex)
        {
            // The unique index on (day, slot) caught someone booking the same time a moment earlier.
            throw new ConflictException(
                $"Someone just booked {TimeSlots.Format(request.StartTime)}. Please pick another time.", ex);
        }

        var patientName = $"{patient.FirstName} {patient.LastName}".Trim();
        await _notificationRepository.AddAsync(new Notification
        {
            UserId = target.DoctorId,
            Message = $"An appointment of {patientName} on {target.Day.ScheduleDate:dd MMM yyyy} at " +
                      $"{TimeSlots.Format(request.StartTime)} is waiting for your approval.",
            CreatedAt = DateTime.UtcNow
        });

        return (await _repository.GetWithDetailsAsync(booked.Id))!;
    }

    public async Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId)
    {
        var user = await _userRepository.GetAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        return user.Role == UserRole.Doctor
            ? await _repository.GetForDoctorAsync(userId)
            : await _repository.GetForPatientAsync(userId);
    }

    public async Task<Appointment> UpdateStatusAsync(int userId, int appointmentId, string status)
    {
        if (!Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var next) || next == AppointmentStatus.Pending)
        {
            throw new ValidationException("Status must be Approved, Declined or Cancelled.");
        }

        var appointment = await _repository.GetWithDetailsAsync(appointmentId);
        var isDoctor = appointment?.DoctorId == userId;
        var isPatient = appointment?.PatientId == userId;
        if (appointment is null || (!isDoctor && !isPatient))
        {
            throw new NotFoundException(nameof(Appointment), appointmentId);
        }

        var current = appointment.Status;
        var allowed = (isDoctor, current, next) switch
        {
            // Doctors answer requests, and can still cancel an approved visit.
            (true, AppointmentStatus.Pending, AppointmentStatus.Approved or AppointmentStatus.Declined) => true,
            (true, AppointmentStatus.Approved, AppointmentStatus.Cancelled) => true,
            // Patients can withdraw a request or cancel an approved visit.
            (false, AppointmentStatus.Pending or AppointmentStatus.Approved, AppointmentStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
        {
            throw new ValidationException($"A {current.ToString().ToLowerInvariant()} appointment can't be changed to {next.ToString().ToLowerInvariant()}.");
        }

        if (next == AppointmentStatus.Cancelled && appointment.AppointmentDate < DateOnly.FromDateTime(DateTime.Now))
        {
            throw new ValidationException("Past appointments can't be cancelled.");
        }

        // Update only the appointment row; the detailed copy above is read-only.
        var entity = (await _repository.GetAsync(appointmentId))!;
        entity.Status = next;
        entity.CancelledById = next == AppointmentStatus.Cancelled ? userId : null;
        await _repository.UpdateAsync(entity);
        return (await _repository.GetWithDetailsAsync(appointmentId))!;
    }
}
