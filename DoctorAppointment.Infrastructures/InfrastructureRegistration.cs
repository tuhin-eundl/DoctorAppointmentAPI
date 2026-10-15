using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Infrastructures.Database;
using DoctorAppointment.Infrastructures.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DoctorAppointment.Infrastructures;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conn));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();

        return services;
    }
}

