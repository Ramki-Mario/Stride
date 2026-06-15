namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Marks an API controller as belonging to a gated module.
/// <see cref="ModuleGateFilter"/> reads this attribute and returns 403 when
/// <see cref="IModuleEntitlementService.IsModuleEnabledAsync"/> returns false.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class RequiresModuleAttribute(string moduleName) : Attribute
{
    public string ModuleName { get; } = moduleName;
}
