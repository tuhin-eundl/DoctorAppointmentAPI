using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures.Database;

public sealed class AppDbContext : AuditDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IExecutionContextAccessor contextAccessor) : base(options, contextAccessor) { }

    public DbSet<User> Users { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<DoctorScheduleDay> DoctorScheduleDays { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
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
    }

    private static void SetSoftDeleteFilter<TEntity>(
        ModelBuilder modelBuilder)
        where TEntity : AuditableEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => !entity.IsDeleted);
    }
}

