using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Application.EventHandlers;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.Tests;

public sealed class OverdueNotificationHandlerTests
{
    // ── StepOverdueNotificationHandler ────────────────────────────────────────

    [Fact]
    public async Task Handle_StepOverdue_SendsNotificationToAssigneeWhenAssigned()
    {
        // Arrange
        var sender   = Substitute.For<ISender>();
        var logger   = NullLogger<StepOverdueNotificationHandler>.Instance;
        var handler  = new StepOverdueNotificationHandler(sender, logger);

        var assigneeId = Guid.NewGuid();
        var startedBy  = Guid.NewGuid();
        var tenantId   = Guid.NewGuid();
        var stepId     = Guid.NewGuid();
        var wfId       = Guid.NewGuid();

        var ev = new StepOverdueEvent(stepId, wfId, tenantId, assigneeId, startedBy);
        var notification = new DomainEventNotification<StepOverdueEvent>(ev);

        sender.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Success(new CreateNotificationResult(Guid.NewGuid())));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — notification should go to assignee
        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c =>
                c.RecipientId == assigneeId
             && c.TenantId    == tenantId
             && c.Type        == NotificationType.StepOverdue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StepOverdue_FallsBackToStartedByWhenNoAssignee()
    {
        // Arrange
        var sender  = Substitute.For<ISender>();
        var logger  = NullLogger<StepOverdueNotificationHandler>.Instance;
        var handler = new StepOverdueNotificationHandler(sender, logger);

        var startedBy = Guid.NewGuid();
        var tenantId  = Guid.NewGuid();

        var ev = new StepOverdueEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, AssigneeId: null, startedBy);
        var notification = new DomainEventNotification<StepOverdueEvent>(ev);

        sender.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Success(new CreateNotificationResult(Guid.NewGuid())));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — no assignee → notify workflow starter
        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c =>
                c.RecipientId == startedBy
             && c.Type        == NotificationType.StepOverdue),
            Arg.Any<CancellationToken>());
    }

    // ── WorkflowSlaBreachedNotificationHandler ────────────────────────────────

    [Fact]
    public async Task Handle_WorkflowSlaBreached_SendsNotificationToStartedBy()
    {
        // Arrange
        var sender  = Substitute.For<ISender>();
        var logger  = NullLogger<WorkflowSlaBreachedNotificationHandler>.Instance;
        var handler = new WorkflowSlaBreachedNotificationHandler(sender, logger);

        var startedBy  = Guid.NewGuid();
        var tenantId   = Guid.NewGuid();
        var deadlineAt = DateTime.UtcNow.AddHours(-1);

        var ev = new WorkflowSlaBreachedEvent(Guid.NewGuid(), tenantId, startedBy, deadlineAt);
        var notification = new DomainEventNotification<WorkflowSlaBreachedEvent>(ev);

        sender.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Success(new CreateNotificationResult(Guid.NewGuid())));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — notification goes to workflow initiator
        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c =>
                c.RecipientId == startedBy
             && c.TenantId    == tenantId
             && c.Type        == NotificationType.WorkflowSlaBreached),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WorkflowSlaBreached_BodyContainsDeadlineTimestamp()
    {
        // Arrange
        var sender  = Substitute.For<ISender>();
        var logger  = NullLogger<WorkflowSlaBreachedNotificationHandler>.Instance;
        var handler = new WorkflowSlaBreachedNotificationHandler(sender, logger);

        var deadlineAt = new DateTime(2026, 6, 10, 14, 30, 0, DateTimeKind.Utc);
        var ev = new WorkflowSlaBreachedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), deadlineAt);
        var notification = new DomainEventNotification<WorkflowSlaBreachedEvent>(ev);

        sender.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Success(new CreateNotificationResult(Guid.NewGuid())));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — body should contain formatted deadline
        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c => c.Body.Contains("10 Jun 2026")),
            Arg.Any<CancellationToken>());
    }
}
