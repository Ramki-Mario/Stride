using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BuildingBlocks.Application.Behaviours;
using System.Reflection;

namespace STRIDE.Modules.Reporting.Application;

public static class ReportingApplicationExtensions
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // Pipeline: Logging → Validation → Handler (consistent with all other modules)
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
