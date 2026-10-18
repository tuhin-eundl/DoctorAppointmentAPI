using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures;

public abstract class AuditDbContext : DbContext
{
    private readonly IExecutionContextAccessor _contextAccessor;

    protected AuditDbContext(DbContextOptions options, IExecutionContextAccessor contextAccessor) : base(options)
    {
        _contextAccessor = contextAccessor;
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var username = _contextAccessor.UserName;
        var now = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>()
                     .Where(e => e.State is EntityState.Added
                         or EntityState.Modified))
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = username;
            }
            else
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = username;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

