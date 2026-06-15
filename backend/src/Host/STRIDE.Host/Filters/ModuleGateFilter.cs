using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.Host.Filters;

/// <summary>
/// Global action filter that enforces per-tenant module entitlements.
/// Controllers decorated with <see cref="RequiresModuleAttribute"/> receive a 403
/// when <see cref="IModuleEntitlementService.IsModuleEnabledAsync"/> returns false.
/// Controllers without the attribute are always allowed through.
/// </summary>
public sealed class ModuleGateFilter(
    IModuleEntitlementService entitlementService,
    ITenantContext tenantContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            var attr = descriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(RequiresModuleAttribute), inherit: true)
                .OfType<RequiresModuleAttribute>()
                .FirstOrDefault();

            if (attr is not null)
            {
                var enabled = await entitlementService.IsModuleEnabledAsync(
                    tenantContext.TenantId, attr.ModuleName, context.HttpContext.RequestAborted);

                if (!enabled)
                {
                    context.Result = new ObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title  = "Module not available",
                        Detail = $"The '{attr.ModuleName}' module is not enabled for your organisation.",
                    })
                    { StatusCode = StatusCodes.Status403Forbidden };
                    return;
                }
            }
        }

        await next();
    }
}
