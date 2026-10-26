using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures.Database;

public sealed class AppDbContext : AuditDbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IExecutionContextAccessor contextAccessor)
        : base(options, contextAccessor)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<DoctorScheduleDay> DoctorScheduleDays { get; set; }
    public DbSet<Hospital> Hospitals { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<Appointment>(appointment =>
        {
            // Users and schedule days are soft-deleted, and SQL Server rejects multiple cascade
            // paths to Users, so none of these relationships cascade.
            appointment.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
            appointment.HasOne(a => a.Doctor).WithMany().HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
            appointment.HasOne(a => a.ScheduleDay).WithMany().HasForeignKey(a => a.ScheduleDayId).OnDelete(DeleteBehavior.Restrict);

            // Two active (Pending/Approved) appointments can never share a serial number on the same day,
            // even if two patients book at the same moment.
            appointment.HasIndex(a => new { a.ScheduleDayId, a.SerialNumber })
                .IsUnique()
                .HasFilter("[Status] IN (0, 1) AND [IsDeleted] = 0");
            appointment.HasIndex(a => new { a.PatientId, a.AppointmentDate });
        });

        modelBuilder.Entity<Notification>(notification =>
        {
            notification.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            notification.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
            notification.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
        });

        modelBuilder.Entity<DoctorSchedule>().Property(s => s.MaxAppointmentsPerDay).HasDefaultValue(20);
        modelBuilder.Entity<DoctorSchedule>().Property(s => s.AverageAppointmentMinutes).HasDefaultValue(15);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(
                        nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, [modelBuilder]);
            }
    }

    private static void SetSoftDeleteFilter<TEntity>(
        ModelBuilder modelBuilder)
        where TEntity : AuditableEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => !entity.IsDeleted);
    }
}

