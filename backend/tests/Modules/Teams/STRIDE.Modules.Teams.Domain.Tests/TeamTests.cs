using STRIDE.Modules.Teams.Domain.Entities;
using STRIDE.Modules.Teams.Domain.Enums;
using STRIDE.Modules.Teams.Domain.Events;
using STRIDE.Modules.Teams.Domain.Exceptions;
using STRIDE.Modules.Teams.Domain.Tests.Builders;

namespace STRIDE.Modules.Teams.Domain.Tests;

public sealed class TeamTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_ReturnsTeamWithActiveStatus()
    {
        var team = new TeamBuilder().WithName("Engineering").Build();

        team.Name.Should().Be("Engineering");
        team.Status.Should().Be(TeamStatus.Active);
        team.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ValidInput_RaisesTeamCreatedEvent()
    {
        var team = new TeamBuilder().Build();

        team.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TeamCreatedEvent>();
    }

    [Fact]
    public void Create_TrimsName()
    {
        var team = new TeamBuilder().WithName("  Engineering  ").Build();

        team.Name.Should().Be("Engineering");
    }

    [Fact]
    public void Create_EmptyName_ThrowsTeamDomainException()
    {
        var act = () => new TeamBuilder().WithName("").Build();

        act.Should().Throw<TeamDomainException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Create_NameTooLong_ThrowsTeamDomainException()
    {
        var act = () => new TeamBuilder().WithName(new string('A', Team.NameMaxLength + 1)).Build();

        act.Should().Throw<TeamDomainException>()
            .WithMessage($"*{Team.NameMaxLength}*");
    }

    [Fact]
    public void Create_DescriptionTooLong_ThrowsTeamDomainException()
    {
        var act = () => new TeamBuilder()
            .WithDescription(new string('x', Team.DescriptionMaxLength + 1))
            .Build();

        act.Should().Throw<TeamDomainException>()
            .WithMessage($"*{Team.DescriptionMaxLength}*");
    }

    [Fact]
    public void Create_NullDescription_IsAccepted()
    {
        var team = new TeamBuilder().WithDescription(null).Build();

        team.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithParentTeamId_SetsParent()
    {
        var parentId = Guid.NewGuid();
        var team = new TeamBuilder().WithParentTeamId(parentId).Build();

        team.ParentTeamId.Should().Be(parentId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ValidInput_UpdatesNameAndDescription()
    {
        var team = new TeamBuilder().Build();
        var updatedBy = Guid.NewGuid();

        team.Update("Backend", "Backend squad", null, updatedBy);

        team.Name.Should().Be("Backend");
        team.Description.Should().Be("Backend squad");
    }

    [Fact]
    public void Update_ValidInput_RaisesTeamUpdatedEvent()
    {
        var team = new TeamBuilder().Build();
        team.ClearDomainEvents();

        team.Update("Backend", null, null, Guid.NewGuid());

        team.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TeamUpdatedEvent>();
    }

    [Fact]
    public void Update_EmptyName_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();

        var act = () => team.Update("", null, null, Guid.NewGuid());

        act.Should().Throw<TeamDomainException>();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ActiveTeam_SetsStatusToInactive()
    {
        var team = new TeamBuilder().Build();

        team.Deactivate(Guid.NewGuid());

        team.Status.Should().Be(TeamStatus.Inactive);
    }

    [Fact]
    public void Deactivate_ActiveTeam_RaisesTeamDeactivatedEvent()
    {
        var team = new TeamBuilder().Build();
        team.ClearDomainEvents();

        team.Deactivate(Guid.NewGuid());

        team.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TeamDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();
        team.Deactivate(Guid.NewGuid());

        var act = () => team.Deactivate(Guid.NewGuid());

        act.Should().Throw<TeamDomainException>()
            .WithMessage("*already inactive*");
    }

    // ── Reactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Reactivate_InactiveTeam_SetsStatusToActive()
    {
        var team = new TeamBuilder().Build();
        team.Deactivate(Guid.NewGuid());

        team.Reactivate(Guid.NewGuid());

        team.Status.Should().Be(TeamStatus.Active);
    }

    [Fact]
    public void Reactivate_InactiveTeam_RaisesTeamReactivatedEvent()
    {
        var team = new TeamBuilder().Build();
        team.Deactivate(Guid.NewGuid());
        team.ClearDomainEvents();

        team.Reactivate(Guid.NewGuid());

        team.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TeamReactivatedEvent>();
    }

    [Fact]
    public void Reactivate_AlreadyActive_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();

        var act = () => team.Reactivate(Guid.NewGuid());

        act.Should().Throw<TeamDomainException>()
            .WithMessage("*already active*");
    }

    // ── Update — additional validation paths ──────────────────────────────────

    [Fact]
    public void Update_NameTooLong_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();

        var act = () => team.Update(new string('Z', Team.NameMaxLength + 1), null, null, Guid.NewGuid());

        act.Should().Throw<TeamDomainException>()
            .WithMessage($"*{Team.NameMaxLength}*");
    }

    [Fact]
    public void Update_DescriptionTooLong_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();

        var act = () => team.Update("Valid", new string('x', Team.DescriptionMaxLength + 1), null, Guid.NewGuid());

        act.Should().Throw<TeamDomainException>()
            .WithMessage($"*{Team.DescriptionMaxLength}*");
    }

    [Fact]
    public void Update_TrimsName()
    {
        var team = new TeamBuilder().Build();

        team.Update("  Platform  ", null, null, Guid.NewGuid());

        team.Name.Should().Be("Platform");
    }

    [Fact]
    public void Update_NullDescription_ClearsDescription()
    {
        var team = new TeamBuilder().WithDescription("old desc").Build();

        team.Update("Name", null, null, Guid.NewGuid());

        team.Description.Should().BeNull();
    }

    [Fact]
    public void Update_SetsNewParentTeamId()
    {
        var parentId = Guid.NewGuid();
        var team     = new TeamBuilder().Build();

        team.Update("Name", null, parentId, Guid.NewGuid());

        team.ParentTeamId.Should().Be(parentId);
    }

    [Fact]
    public void Update_ClearsParentTeamId_WhenNullPassed()
    {
        var team = new TeamBuilder().WithParentTeamId(Guid.NewGuid()).Build();

        team.Update("Name", null, null, Guid.NewGuid());

        team.ParentTeamId.Should().BeNull();
    }

    // ── Create — additional property checks ───────────────────────────────────

    [Fact]
    public void Create_WhitespaceOnlyName_ThrowsTeamDomainException()
    {
        var act = () => new TeamBuilder().WithName("   ").Build();

        act.Should().Throw<TeamDomainException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Create_SetsAllCoreProperties()
    {
        var tenantId = Guid.NewGuid();
        var team = Team.Create(new NewTeam(tenantId, "Engineering", "Core squad", null, Guid.NewGuid()));

        team.TenantId.Should().Be(tenantId);
        team.Name.Should().Be("Engineering");
        team.Description.Should().Be("Core squad");
        team.ParentTeamId.Should().BeNull();
        team.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_TrimsDescription()
    {
        var team = new TeamBuilder().WithDescription("  Squad  ").Build();

        team.Description.Should().Be("Squad");
    }

    // ── Deleted-team guard paths ──────────────────────────────────────────────

    [Fact]
    public void Update_DeletedTeam_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();
        MarkDeleted(team);

        var act = () => team.Update("New Name", null, null, Guid.NewGuid());

        act.Should().Throw<TeamDomainException>()
            .WithMessage("*deleted*");
    }

    [Fact]
    public void Deactivate_DeletedTeam_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();
        MarkDeleted(team);

        var act = () => team.Deactivate(Guid.NewGuid());

        act.Should().Throw<TeamDomainException>()
            .WithMessage("*deleted*");
    }

    [Fact]
    public void Reactivate_DeletedTeam_ThrowsTeamDomainException()
    {
        var team = new TeamBuilder().Build();
        MarkDeleted(team);

        var act = () => team.Reactivate(Guid.NewGuid());

        act.Should().Throw<TeamDomainException>()
            .WithMessage("*deleted*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Uses reflection to set the protected <c>IsDeleted</c> flag so we can
    /// exercise the guard branches without requiring a real soft-delete operation.
    /// </summary>
    private static void MarkDeleted(Team team)
        => team.GetType().GetProperty("IsDeleted")!.SetValue(team, true);
}
