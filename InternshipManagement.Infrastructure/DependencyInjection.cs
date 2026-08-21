using Microsoft.Extensions.DependencyInjection;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Application.Services;

namespace InternshipManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Register services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IInternshipService, InternshipService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<ICompanyService, CompanyService>();

            return services;
        }
    }
}