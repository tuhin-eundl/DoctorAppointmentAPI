using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace DoctorAppointment.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
