using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures.Database;

public sealed class AppDbContext : AuditDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IExecutionContextAccessor contextAccessor) : base(options, contextAccessor) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
    }
}

