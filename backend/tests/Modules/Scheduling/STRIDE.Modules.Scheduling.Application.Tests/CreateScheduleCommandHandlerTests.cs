using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;
using STRIDE.Modules.Scheduling.Domain.Entities;

namespace STRIDE.Modules.Scheduling.Application.Tests;

public sealed class CreateScheduleCommandHandlerTests
{
    private static readonly Guid   TenantId = Guid.NewGuid();
    private static readonly Guid   UserId   = Guid.NewGuid();

    private readonly IScheduleDefinitionRepository _repository   = Substitute.For<IScheduleDefinitionRepository>();
    private readonly ICurrentUser                  _currentUser  = Substitute.For<ICurrentUser>();
    private readonly ITenantContext                _tenantContext = Substitute.For<ITenantContext>();

    private readonly CreateScheduleCommandHandler _sut;

    public CreateScheduleCommandHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _tenantContext.TenantId.Returns(TenantId);
        _sut = new CreateScheduleCommandHandler(_repository, _currentUser, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithValidActiveCommand_ReturnsSuccessWithDto()
    {
        var result = await _sut.Handle(BuildCommand(isActive: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Daily Report");
        result.Value.IsActive.Should().BeTrue();
        result.Value.NextRunAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithValidInactiveCommand_ReturnsSuccessWithNullNextRunAt()
    {
        var result = await _sut.Handle(BuildCommand(isActive: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        result.Value.NextRunAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsScheduleToRepository()
    {
        await _sut.Handle(BuildCommand(), CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<ScheduleDefinition>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsTenantIdFromContext()
    {
        await _sut.Handle(BuildCommand(), CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<ScheduleDefinition>(s => s.TenantId == TenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ComputeNextRun_WithValidCron_ReturnsNonNullFutureDate()
    {
        var result = CreateScheduleCommandHandler.ComputeNextRun("0 9 * * 1-5");

        result.Should().NotBeNull();
        result!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void ComputeNextRun_WithInvalidCron_ReturnsNull()
    {
        var result = CreateScheduleCommandHandler.ComputeNextRun("not-a-cron");

        result.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateScheduleCommand BuildCommand(bool isActive = true) =>
        new(
            Name:                 "Daily Report",
            Description:          null,
            WorkflowDefinitionId: Guid.NewGuid(),
            CronExpression:       "0 9 * * 1-5",
            IsActive:             isActive);
}
