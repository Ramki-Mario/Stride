using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Reporting.Application;
using STRIDE.Modules.Reporting.Infrastructure;

namespace STRIDE.Modules.Reporting.API.Extensions;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReportingApplication();
        services.AddReportingInfrastructure(configuration);
        return services;
    }
}
