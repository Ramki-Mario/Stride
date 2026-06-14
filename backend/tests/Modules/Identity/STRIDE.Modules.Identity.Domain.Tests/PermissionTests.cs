using FluentAssertions;
using STRIDE.Modules.Identity.Domain;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Tests;

public sealed class PermissionTests
{
    [Fact]
    public void Create_WithValidKey_SetsProperties()
    {
        var permission = Permission.Create("workflow.view", "View workflow definitions");

        permission.Key.Should().Be("workflow.view");
        permission.Description.Should().Be("View workflow definitions");
        permission.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TrimsAndLowercasesKey()
    {
        var permission = Permission.Create("  WORKFLOW.VIEW  ", "desc");

        permission.Key.Should().Be("workflow.view");
    }

    [Fact]
    public void Create_TrimsDescription()
    {
        var permission = Permission.Create("role.view", "  View the tenant role catalog  ");

        permission.Description.Should().Be("View the tenant role catalog");
    }

    [Theory]
    [InlineData("workflow.view")]
    [InlineData("workflow.create")]
    [InlineData("workflow.activate")]
    [InlineData("workflow.run")]
    [InlineData("workflow.manage_instances")]
    [InlineData("user.invite")]
    [InlineData("user.manage")]
    [InlineData("role.view")]
    [InlineData("role.manage")]
    [InlineData("tenant.settings")]
    public void DefaultPermissions_All_ContainsExpectedKey(string key)
    {
        DefaultPermissions.All.Should().Contain(p => p.Key == key);
    }

    [Fact]
    public void DefaultPermissions_All_HasTenEntries()
    {
        DefaultPermissions.All.Should().HaveCount(10);
    }

    [Fact]
    public void DefaultPermissions_All_HasUniqueIds()
    {
        var ids = DefaultPermissions.All.Select(p => p.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().AllSatisfy(id => id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public void DefaultPermissions_All_HasUniqueKeys()
    {
        var keys = DefaultPermissions.All.Select(p => p.Key).ToList();
        keys.Should().OnlyHaveUniqueItems();
    }
}
