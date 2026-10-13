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

    public async Task<int> SaveChangesAsync(string userName = "")
    {
        var username = string.IsNullOrEmpty(userName) ? _contextAccessor.UserName : userName;
        foreach (var entry in base.ChangeTracker.Entries<AuditableEntity>()
                     .Where(q => q.State
                         is EntityState.Added
                         or EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.Now;
            entry.Entity.UpdatedBy = username;

            if (entry.State != EntityState.Added) continue;

            entry.Entity.CreatedAt = DateTime.Now;
            entry.Entity.CreatedBy = username;
        }

        var result = await base.SaveChangesAsync();

        return result;
    }
}

