using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Abstractions;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Application.EventHandlers;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Entities;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.Tests;

/// <summary>
/// US-172 coverage: ApprovalRequestedEvent must fan out in-app + Web Push
/// notifications to every user holding the required approver role, and do
/// nothing when no role is configured or nobody holds it.
/// </summary>
public sealed class ApprovalRequestedNotificationHandlerTests
{
    private readonly ISender                     _sender    = Substitute.For<ISender>();
    private readonly IUserRoleQueryService       _userRoles = Substitute.For<IUserRoleQueryService>();
    private readonly IPushSubscriptionRepository _pushSubs  = Substitute.For<IPushSubscriptionRepository>();
    private readonly IWebPushService             _webPush   = Substitute.For<IWebPushService>();

    private readonly ApprovalRequestedNotificationHandler _sut;

    public ApprovalRequestedNotificationHandlerTests()
    {
        _sut = new ApprovalRequestedNotificationHandler(
            _sender, _userRoles, _pushSubs, _webPush,
            NullLogger<ApprovalRequestedNotificationHandler>.Instance);

        _sender.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
               .Returns(Result.Success(new CreateNotificationResult(Guid.NewGuid())));

        _pushSubs.GetByUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Array.Empty<PushSubscription>());
    }

    private static DomainEventNotification<ApprovalRequestedEvent> BuildNotification(
        Guid? requiredRoleId,
        Guid tenantId,
        string workflowName = "Onboarding")
    {
        var ev = new ApprovalRequestedEvent(
            Guid.NewGuid(), Guid.NewGuid(), tenantId, requiredRoleId, "Final Review", workflowName);
        return new DomainEventNotification<ApprovalRequestedEvent>(ev);
    }

    [Fact]
    public async Task Handle_WhenNoRequiredRole_DoesNothing()
    {
        await _sut.Handle(BuildNotification(requiredRoleId: null, Guid.NewGuid()), CancellationToken.None);

        await _userRoles.DidNotReceive().GetUsersInRoleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().Send(
            Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoUsersHoldRole_DoesNothing()
    {
        var roleId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _userRoles.GetUsersInRoleAsync(roleId, tenantId, Arg.Any<CancellationToken>())
                  .Returns(Array.Empty<Guid>());

        await _sut.Handle(BuildNotification(roleId, tenantId), CancellationToken.None);

        await _sender.DidNotReceive().Send(
            Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SendsApprovalRequestedNotificationToEveryApprover()
    {
        var roleId    = Guid.NewGuid();
        var tenantId  = Guid.NewGuid();
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();

        _userRoles.GetUsersInRoleAsync(roleId, tenantId, Arg.Any<CancellationToken>())
                  .Returns(new[] { approver1, approver2 });

        await _sut.Handle(BuildNotification(roleId, tenantId), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c =>
                c.RecipientId == approver1
             && c.TenantId    == tenantId
             && c.Type        == NotificationType.ApprovalRequested),
            Arg.Any<CancellationToken>());

        await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c => c.RecipientId == approver2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BodyMentionsStepAndWorkflowName()
    {
        var roleId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _userRoles.GetUsersInRoleAsync(roleId, tenantId, Arg.Any<CancellationToken>())
                  .Returns(new[] { Guid.NewGuid() });

        await _sut.Handle(
            BuildNotification(roleId, tenantId, workflowName: "Client Onboarding"),
            CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c =>
                c.Body.Contains("Final Review") && c.Body.Contains("Client Onboarding")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SendsWebPushToEachApproverSubscription()
    {
        var roleId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var approver = Guid.NewGuid();

        var subscription = PushSubscription.Create(
            tenantId, approver, "https://push.example/ep", "p256dh-key", "auth-secret");

        _userRoles.GetUsersInRoleAsync(roleId, tenantId, Arg.Any<CancellationToken>())
                  .Returns(new[] { approver });
        _pushSubs.GetByUserAsync(tenantId, approver, Arg.Any<CancellationToken>())
                 .Returns(new[] { subscription });

        await _sut.Handle(BuildNotification(roleId, tenantId), CancellationToken.None);

        await _webPush.Received(1).SendAsync(
            subscription, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
