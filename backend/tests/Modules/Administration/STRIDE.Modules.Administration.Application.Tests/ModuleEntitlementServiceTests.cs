using FluentAssertions;
using NSubstitute;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Domain.Entities;
using STRIDE.Modules.Administration.Infrastructure.Services;

namespace STRIDE.Modules.Administration.Application.Tests;

public sealed class ModuleEntitlementServiceTests
{
    private readonly ITenantSettingsRepository _repo = Substitute.For<ITenantSettingsRepository>();
    private readonly ModuleEntitlementService  _sut;

    public ModuleEntitlementServiceTests()
    {
        _sut = new ModuleEntitlementService(_repo);
    }

    [Fact]
    public async Task IsModuleEnabledAsync_NoSettingsRow_ReturnsTrue()
    {
        _repo.GetByTenantIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((TenantSettings?)null);

        var result = await _sut.IsModuleEnabledAsync(Guid.NewGuid(), "KitOps");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsModuleEnabledAsync_NullEnabledModules_ReturnsTrue()
    {
        var settings = TenantSettings.CreateDefaults(Guid.NewGuid(), Guid.NewGuid());
        // EnabledModules is null by default — all modules enabled.
        _repo.GetByTenantIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(settings);

        var result = await _sut.IsModuleEnabledAsync(Guid.NewGuid(), "KitOps");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsModuleEnabledAsync_ModuleInList_ReturnsTrue()
    {
        var settings = TenantSettings.CreateDefaults(Guid.NewGuid(), Guid.NewGuid());
        settings.SetEnabledModules(["KitOps", "Reporting"]);
        _repo.GetByTenantIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(settings);

        var result = await _sut.IsModuleEnabledAsync(Guid.NewGuid(), "KitOps");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsModuleEnabledAsync_ModuleNotInList_ReturnsFalse()
    {
        var settings = TenantSettings.CreateDefaults(Guid.NewGuid(), Guid.NewGuid());
        settings.SetEnabledModules(["Reporting"]);
        _repo.GetByTenantIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(settings);

        var result = await _sut.IsModuleEnabledAsync(Guid.NewGuid(), "KitOps");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsModuleEnabledAsync_PassesTenantIdToRepository()
    {
        var tenantId = Guid.NewGuid();
        _repo.GetByTenantIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((TenantSettings?)null);

        await _sut.IsModuleEnabledAsync(tenantId, "KitOps");

        await _repo.Received(1).GetByTenantIdAsync(tenantId, Arg.Any<CancellationToken>());
    }
}
