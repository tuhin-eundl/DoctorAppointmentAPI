using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Appointments;
using DoctorAppointment.Application.Exceptions;
using DoctorAppointment.Domain;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DoctorAppointment.Application.UseCases;

public sealed class AppointmentService : IAppointmentService
{
    private const string Free = "Free";
    private const string Booked = "Booked";
    private const string Unavailable = "Unavailable";
    private const int MaxReasonLength = 500;

    private readonly IAppointmentRepository _repository;
    private readonly IDoctorScheduleRepository _scheduleRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public AppointmentService(
        IAppointmentRepository repository,
        IDoctorScheduleRepository scheduleRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _repository = repository;
        _scheduleRepository = scheduleRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    private static string FullName(User? user) =>
        user is null ? "A user" : $"{user.FirstName} {user.LastName}".Trim();

    private static string DoctorName(User? doctor) => doctor is null ? "Your doctor" : $"Dr. {FullName(doctor)}";

    /// <summary>"Mon, Oct 27 at 10:30 AM"</summary>
    private static string When(DateOnly date, TimeOnly time) =>
        $"{date.ToString("ddd, MMM d", CultureInfo.InvariantCulture)} at {TimeSlots.Format(time)}";

    private static string WithReason(string message, string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? message : $"{message} Reason: {reason}";

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

    /// <summary>Every slot of the day with its state, given the slot numbers already approved for other patients.</summary>
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

    /// <summary>
    /// Approved slot numbers per day. Pending requests don't take a slot: several patients can ask for
    /// the same time, and approving one closes the others.
    /// </summary>
    private async Task<Dictionary<int, HashSet<int>>> TakenSlotsAsync(IEnumerable<BookableDay> days)
    {
        var approved = await _repository.GetApprovedOnDaysAsync(days.Select(x => x.Day.Id).ToList());
        return approved
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

        if (await _repository.PatientHasActiveAppointmentAsync(patientId, target.DoctorId, target.Day.ScheduleDate, request.StartTime))
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

        var appointment = await _repository.AddAsync(new Appointment
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

        await _notificationService.NotifyAsync(
            target.DoctorId,
            $"{FullName(patient)} requested an appointment on {When(target.Day.ScheduleDate, request.StartTime)}. " +
            "It's waiting for your approval.");

        return (await _repository.GetWithDetailsAsync(appointment.Id))!;
    }

    public async Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId)
    {
        var user = await _userRepository.GetAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        // Statuses are brought up to date on every read, so a missed visit always shows as Overdue.
        await _repository.MarkOverdueAsync(DateTime.Now);
        return user.Role == UserRole.Doctor
            ? await _repository.GetForDoctorAsync(userId)
            : await _repository.GetForPatientAsync(userId);
    }

    public async Task<Appointment> UpdateStatusAsync(int userId, int appointmentId, string status, string? reason)
    {
        if (!Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var next) ||
            next is AppointmentStatus.Pending or AppointmentStatus.SlotTaken or AppointmentStatus.Overdue)
        {
            throw new ValidationException("Status must be Approved, Declined or Cancelled.");
        }

        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (reason?.Length > MaxReasonLength)
        {
            throw new ValidationException($"Keep the reason to {MaxReasonLength} characters or fewer.");
        }

        // An appointment whose time has passed can't be approved or cancelled any more.
        await _repository.MarkOverdueAsync(DateTime.Now);
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
        if (!allowed && current == AppointmentStatus.Overdue)
        {
            throw new ValidationException("This appointment's time has already passed, so it can't be changed.");
        }
        if (!allowed && current == AppointmentStatus.SlotTaken)
        {
            throw new ValidationException("This request was closed because another patient was approved for the same time.");
        }
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
        var closedPatientIds = new List<int>();
        if (next == AppointmentStatus.Approved)
        {
            await EnsureCanApproveAsync(entity);
            // The closed requests' reason becomes the explanation for those patients.
            var conflictReason =
                $"Cancelled due to a schedule conflict: the doctor confirmed another patient for " +
                $"{TimeSlots.Format(entity.StartTime)} on {entity.AppointmentDate.ToString("MMM d", CultureInfo.InvariantCulture)}. " +
                "Please book another time.";
            foreach (var other in await _repository.GetPendingInSlotAsync(entity.ScheduleDayId, entity.SerialNumber, entity.Id))
            {
                other.Status = AppointmentStatus.SlotTaken;
                other.Reason = conflictReason;
                closedPatientIds.Add(other.PatientId);
            }
        }

        entity.Status = next;
        entity.CancelledById = next == AppointmentStatus.Cancelled ? userId : null;
        // When the doctor declines or cancels, the reason becomes their explanation for the patient.
        if (isDoctor && next is AppointmentStatus.Declined or AppointmentStatus.Cancelled)
        {
            entity.Reason = reason;
        }
        // Also saves the closed requests for the same slot, which the same context is tracking.
        await _repository.UpdateAsync(entity);

        await NotifyStatusChangeAsync(appointment, current, next, isDoctor, entity.Reason, closedPatientIds);
        return (await _repository.GetWithDetailsAsync(appointmentId))!;
    }

    /// <summary>Tells the other side of the appointment what changed, and patients whose requests were closed.</summary>
    private async Task NotifyStatusChangeAsync(
        Appointment appointment,
        AppointmentStatus previous,
        AppointmentStatus next,
        bool byDoctor,
        string? reason,
        IReadOnlyCollection<int> closedPatientIds)
    {
        var when = When(appointment.AppointmentDate, appointment.StartTime);
        var doctor = DoctorName(appointment.Doctor);

        if (byDoctor)
        {
            var message = next switch
            {
                AppointmentStatus.Approved => $"{doctor} approved your appointment on {when}.",
                AppointmentStatus.Declined => WithReason($"{doctor} declined your appointment request for {when}.", reason),
                _ => WithReason($"{doctor} cancelled your appointment on {when}.", reason)
            };
            await _notificationService.NotifyAsync(appointment.PatientId, message);
        }
        else
        {
            var patient = FullName(appointment.Patient);
            var message = previous == AppointmentStatus.Pending
                ? $"{patient} withdrew their appointment request for {when}."
                : $"{patient} cancelled their appointment on {when}.";
            await _notificationService.NotifyAsync(appointment.DoctorId, message);
        }

        if (closedPatientIds.Count > 0)
        {
            await _notificationService.NotifyManyAsync(
                closedPatientIds,
                $"Your request with {doctor} for {when} was closed because the doctor confirmed another patient " +
                "for that time. Please book another time.");
        }
    }

    /// <summary>The slot must not already be approved for someone else, and the day must have room left.</summary>
    private async Task EnsureCanApproveAsync(Appointment appointment)
    {
        var day = (await UpcomingDaysAsync(scheduleDayId: appointment.ScheduleDayId)).SingleOrDefault()
            ?? throw new ValidationException("This day has already passed or was removed from the schedule.");

        var approved = await _repository.GetApprovedOnDaysAsync([appointment.ScheduleDayId]);
        if (approved.Any(a => a.SerialNumber == appointment.SerialNumber))
        {
            throw new ConflictException(
                $"You already approved another patient for {TimeSlots.Format(appointment.StartTime)}. Decline this request instead.");
        }
        if (approved.Count >= day.Capacity)
        {
            throw new ConflictException(
                $"You've reached your limit of {day.Capacity} patients for this day. Decline this request, or raise the limit in your schedule.");
        }
    }
}
