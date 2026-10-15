using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.Logging;
using DoctorAppointment.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace DoctorAppointment.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IExecutionContextAccessor, ExecutionContextAccessor>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();

        return services;
    }
}
