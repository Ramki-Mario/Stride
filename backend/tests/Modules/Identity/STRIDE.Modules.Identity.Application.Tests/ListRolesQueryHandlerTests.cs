using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Queries.ListRoles;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class ListRolesQueryHandlerTests
{
    private readonly IRoleRepository              _roles = Substitute.For<IRoleRepository>();
    private readonly ListRolesQueryHandler        _sut;

    public ListRolesQueryHandlerTests()
    {
        _sut = new ListRolesQueryHandler(_roles, NullLogger<ListRolesQueryHandler>.Instance);
    }

    // ── Empty result ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoRoles_ReturnsSuccessWithEmptyList()
    {
        _roles.GetAllAsync(Arg.Any<CancellationToken>())
              .Returns(Array.Empty<Role>() as IReadOnlyList<Role>);

        var result = await _sut.Handle(new ListRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── Maps fields ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MapsIdNameAndDescription()
    {
        var tenantId = Guid.NewGuid();
        var role     = Role.Create(tenantId, "Field Worker", "Can execute field tasks", Guid.NewGuid());
        _roles.GetAllAsync(Arg.Any<CancellationToken>())
              .Returns(new[] { role } as IReadOnlyList<Role>);

        var result = await _sut.Handle(new ListRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Id.Should().Be(role.Id);
        dto.Name.Should().Be("Field Worker");
        dto.Description.Should().Be("Can execute field tasks");
    }

    // ── Ordering ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnRolesOrderedByName()
    {
        var tenantId = Guid.NewGuid();
        var creator  = Guid.NewGuid();
        var roles = new[]
        {
            Role.Create(tenantId, "Supervisor",         "Team supervisor",      creator),
            Role.Create(tenantId, "Admin",              "System administrator", creator),
            Role.Create(tenantId, "OperationsManager",  "Operations manager",   creator),
        } as IReadOnlyList<Role>;

        _roles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(roles);

        var result = await _sut.Handle(new ListRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(r => r.Name)
              .Should().BeInAscendingOrder();
    }

    // ── Multiple roles ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithMultipleRoles_ReturnAll()
    {
        var tenantId = Guid.NewGuid();
        var creator  = Guid.NewGuid();
        var roles = Enumerable.Range(1, 5)
            .Select(i => Role.Create(tenantId, $"Role {i}", $"Description {i}", creator))
            .ToList() as IReadOnlyList<Role>;

        _roles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(roles);

        var result = await _sut.Handle(new ListRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }
}
