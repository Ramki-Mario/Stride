using STRIDE.BuildingBlocks.Infrastructure.Persistence;

namespace STRIDE.Modules.Administration.Infrastructure.Services;

/// <summary>
/// Loads the <c>stride-theme-template.css</c> embedded resource at startup.
/// </summary>
public static class CssTemplateProvider
{
    private static readonly string _template = SqlLoader.Load(
        typeof(CssTemplateProvider).Assembly,
        "STRIDE.Modules.Administration.Infrastructure.Resources.stride-theme-template.css");

    public static string GetTemplate() => _template;
}
