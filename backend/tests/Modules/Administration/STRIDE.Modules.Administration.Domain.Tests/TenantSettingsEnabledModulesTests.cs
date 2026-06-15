using FluentAssertions;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Domain.Tests;

public sealed class TenantSettingsEnabledModulesTests
{
    private static TenantSettings CreateSettings() =>
        TenantSettings.CreateDefaults(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void IsModuleEnabled_NullEnabledModules_ReturnsTrue()
    {
        var settings = CreateSettings();
        // EnabledModules is null by default (all modules allowed).
        settings.IsModuleEnabled("KitOps").Should().BeTrue();
    }

    [Fact]
    public void IsModuleEnabled_ModuleInList_ReturnsTrue()
    {
        var settings = CreateSettings();
        settings.SetEnabledModules(["KitOps", "Reporting"]);

        settings.IsModuleEnabled("KitOps").Should().BeTrue();
        settings.IsModuleEnabled("Reporting").Should().BeTrue();
    }

    [Fact]
    public void IsModuleEnabled_ModuleNotInList_ReturnsFalse()
    {
        var settings = CreateSettings();
        settings.SetEnabledModules(["Reporting"]);

        settings.IsModuleEnabled("KitOps").Should().BeFalse();
    }

    [Fact]
    public void IsModuleEnabled_IsCaseInsensitive()
    {
        var settings = CreateSettings();
        settings.SetEnabledModules(["kitops"]);

        settings.IsModuleEnabled("KitOps").Should().BeTrue();
    }

    [Fact]
    public void SetEnabledModules_EmptyCollection_SetsNullGrantingAll()
    {
        var settings = CreateSettings();
        settings.SetEnabledModules(["KitOps"]);
        settings.SetEnabledModules([]);

        settings.IsModuleEnabled("KitOps").Should().BeTrue();
        settings.IsModuleEnabled("Anything").Should().BeTrue();
    }

    [Fact]
    public void SetEnabledModules_Null_SetsNullGrantingAll()
    {
        var settings = CreateSettings();
        settings.SetEnabledModules(["KitOps"]);
        settings.SetEnabledModules(null);

        settings.IsModuleEnabled("Anything").Should().BeTrue();
    }

    [Fact]
    public void GetEnabledModulesList_NullEnabledModules_ReturnsEmpty()
    {
        var settings = CreateSettings();
        settings.GetEnabledModulesList().Should().BeEmpty();
    }

    [Fact]
    public void GetEnabledModulesList_WithModules_ReturnsCorrectList()
    {
        var settings = CreateSettings();
        settings.SetEnabledModules(["KitOps", "Reporting"]);

        settings.GetEnabledModulesList().Should().BeEquivalentTo(["KitOps", "Reporting"]);
    }
}
