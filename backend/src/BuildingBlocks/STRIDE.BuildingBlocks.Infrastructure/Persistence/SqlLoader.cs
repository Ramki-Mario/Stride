using System.Reflection;

namespace STRIDE.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Loads embedded SQL resource files from a given assembly.
/// Convention: resource names follow the assembly default namespace + folder path + filename.
/// Example: STRIDE.Modules.Identity.Infrastructure.Tenant.Queries.ResolveTenantByCorporateDomain.sql
/// </summary>
public static class SqlLoader
{
    public static string Load(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded SQL resource '{resourceName}' not found in assembly '{assembly.GetName().Name}'. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
