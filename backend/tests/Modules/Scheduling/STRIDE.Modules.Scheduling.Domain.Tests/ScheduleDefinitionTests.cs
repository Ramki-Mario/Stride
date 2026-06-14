using STRIDE.Modules.Scheduling.Domain.Entities;
using STRIDE.Modules.Scheduling.Domain.Events;
using STRIDE.Modules.Scheduling.Domain.Exceptions;
using STRIDE.Modules.Scheduling.Domain.ValueObjects;

namespace STRIDE.Modules.Scheduling.Domain.Tests;

public sealed class ScheduleDefinitionTests
{
    private static readonly Guid TenantId   = Guid.NewGuid();
    private static readonly Guid WfDefId    = Guid.NewGuid();
    private static readonly Guid ActorId    = Guid.NewGuid();
    private static readonly CronExpression Cron = CronExpression.Create("0 9 * * 1-5");

    // ── CronExpression value object ──────────────────────────────────────────

    [Fact]
    public void CronExpression_Create_WithValid5FieldExpression_Succeeds()
    {
        var expr = CronExpression.Create("0 9 * * 1-5");
        expr.Value.Should().Be("0 9 * * 1-5");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CronExpression_Create_WithEmpty_Throws(string input)
    {
        var act = () => CronExpression.Create(input);
        act.Should().Throw<SchedulingDomainException>().WithMessage("*empty*");
    }

    [Theory]
    [InlineData("0 9")]
    [InlineData("0 9 * * 1-5 extra")]
    public void CronExpression_Create_WithWrongFieldCount_Throws(string input)
    {
        var act = () => CronExpression.Create(input);
        act.Should().Throw<SchedulingDomainException>().WithMessage("*5 fields*");
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsSchedule()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, true, null, ActorId);

        schedule.Id.Should().NotBeEmpty();
        schedule.TenantId.Should().Be(TenantId);
        schedule.Name.Should().Be("Daily");
        schedule.WorkflowDefinitionId.Should().Be(WfDefId);
        schedule.CronExpression.Should().Be(Cron);
        schedule.IsActive.Should().BeTrue();
        schedule.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        var act = () => ScheduleDefinition.Create(TenantId, name, null, WfDefId, Cron, true, null, ActorId);
        act.Should().Throw<SchedulingDomainException>().WithMessage("*name cannot be empty*");
    }

    [Fact]
    public void Create_RaisesScheduleDefinitionCreatedEvent()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, true, null, ActorId);

        schedule.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ScheduleDefinitionCreatedEvent>();
    }

    [Fact]
    public void Create_TrimsName()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "  Daily  ", null, WfDefId, Cron, true, null, ActorId);
        schedule.Name.Should().Be("Daily");
    }

    // ── Activate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenInactive_SetsIsActiveTrue()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, false, null, ActorId);
        schedule.ClearDomainEvents();

        schedule.Activate(DateTime.UtcNow.AddHours(1), ActorId);

        schedule.IsActive.Should().BeTrue();
        schedule.NextRunAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenInactive_RaisesScheduleActivatedEvent()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, false, null, ActorId);
        schedule.ClearDomainEvents();

        schedule.Activate(null, ActorId);

        schedule.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ScheduleActivatedEvent>();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, true, null, ActorId);

        var act = () => schedule.Activate(null, ActorId);
        act.Should().Throw<SchedulingDomainException>().WithMessage("*already active*");
    }

    [Fact]
    public void Activate_WhenDeleted_Throws()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, false, null, ActorId);
        schedule.SoftDelete();

        var act = () => schedule.Activate(null, ActorId);
        act.Should().Throw<SchedulingDomainException>().WithMessage("*deleted*");
    }

    // ── Deactivate ───────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveFalseAndClearsNextRunAt()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, true, DateTime.UtcNow, ActorId);
        schedule.ClearDomainEvents();

        schedule.Deactivate(ActorId);

        schedule.IsActive.Should().BeFalse();
        schedule.NextRunAt.Should().BeNull();
    }

    [Fact]
    public void Deactivate_WhenActive_RaisesScheduleDeactivatedEvent()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, true, null, ActorId);
        schedule.ClearDomainEvents();

        schedule.Deactivate(ActorId);

        schedule.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ScheduleDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, false, null, ActorId);

        var act = () => schedule.Deactivate(ActorId);
        act.Should().Throw<SchedulingDomainException>().WithMessage("*already inactive*");
    }

    [Fact]
    public void Deactivate_WhenDeleted_Throws()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, true, null, ActorId);
        schedule.SoftDelete();

        var act = () => schedule.Deactivate(ActorId);
        act.Should().Throw<SchedulingDomainException>().WithMessage("*deleted*");
    }

    // ── SoftDelete ───────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsDeletedTrueAndIsActiveFalse()
    {
        var schedule = ScheduleDefinition.Create(TenantId, "Daily", null, WfDefId, Cron, true, null, ActorId);

        schedule.SoftDelete();

        schedule.IsDeleted.Should().BeTrue();
        schedule.IsActive.Should().BeFalse();
    }
}
