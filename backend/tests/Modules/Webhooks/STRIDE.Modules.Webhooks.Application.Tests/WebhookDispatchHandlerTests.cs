using Microsoft.Extensions.Logging;
using NSubstitute;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Identity.Domain.Events;
using STRIDE.Modules.Invoicing.Domain.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.EventHandlers;
using STRIDE.Modules.Webhooks.Domain;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Webhooks.Application.Tests;

/// <summary>
/// Tests that each domain event handler correctly calls <see cref="IWebhookDispatcher"/>
/// with the right event type key and does not propagate dispatcher exceptions.
/// </summary>
public sealed class WebhookDispatchHandlerTests
{
    private readonly IWebhookDispatcher _dispatcher = Substitute.For<IWebhookDispatcher>();
    private readonly Guid               _tenantId   = Guid.NewGuid();

    // ── workflow.instance.started ────────────────────────────────────────────

    [Fact]
    public async Task WorkflowStarted_CallsDispatcherWithCorrectEventType()
    {
        var handler = new WorkflowInstanceStartedDispatchHandler(
            _dispatcher, Substitute.For<ILogger<WorkflowInstanceStartedDispatchHandler>>());

        var e = new WorkflowStartedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), "Invoice Flow", Guid.NewGuid());
        await handler.Handle(new DomainEventNotification<WorkflowStartedEvent>(e), CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            _tenantId, WebhookEventTypes.WorkflowInstanceStarted, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WorkflowStarted_DispatcherThrows_DoesNotPropagate()
    {
        _dispatcher.DispatchAsync(default, default!, default!, default)
            .ReturnsForAnyArgs(_ => throw new InvalidOperationException("dispatch failed"));

        var handler = new WorkflowInstanceStartedDispatchHandler(
            _dispatcher, Substitute.For<ILogger<WorkflowInstanceStartedDispatchHandler>>());

        var e = new WorkflowStartedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), "Flow", Guid.NewGuid());
        var act = () => handler.Handle(new DomainEventNotification<WorkflowStartedEvent>(e), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── workflow.instance.completed ──────────────────────────────────────────

    [Fact]
    public async Task WorkflowCompleted_CallsDispatcherWithCorrectEventType()
    {
        var handler = new WorkflowInstanceCompletedDispatchHandler(
            _dispatcher, Substitute.For<ILogger<WorkflowInstanceCompletedDispatchHandler>>());

        var e = new WorkflowCompletedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), "Invoice Flow");
        await handler.Handle(new DomainEventNotification<WorkflowCompletedEvent>(e), CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            _tenantId, WebhookEventTypes.WorkflowInstanceCompleted, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── workflow.step.completed ───────────────────────────────────────────────

    [Fact]
    public async Task StepCompleted_CallsDispatcherWithCorrectEventType()
    {
        var handler = new WorkflowStepCompletedDispatchHandler(
            _dispatcher, Substitute.For<ILogger<WorkflowStepCompletedDispatchHandler>>());

        var e = new StepCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), _tenantId, Guid.NewGuid(), "Sign off");
        await handler.Handle(new DomainEventNotification<StepCompletedEvent>(e), CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            _tenantId, WebhookEventTypes.WorkflowStepCompleted, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── workflow.step.overdue ─────────────────────────────────────────────────

    [Fact]
    public async Task StepOverdue_CallsDispatcherWithCorrectEventType()
    {
        var handler = new WorkflowStepOverdueDispatchHandler(
            _dispatcher, Substitute.For<ILogger<WorkflowStepOverdueDispatchHandler>>());

        var e = new StepOverdueEvent(Guid.NewGuid(), Guid.NewGuid(), _tenantId, null, Guid.NewGuid(), "Review");
        await handler.Handle(new DomainEventNotification<StepOverdueEvent>(e), CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            _tenantId, WebhookEventTypes.WorkflowStepOverdue, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── invoice.created ───────────────────────────────────────────────────────

    [Fact]
    public async Task InvoiceCreated_CallsDispatcherWithCorrectEventType()
    {
        var handler = new InvoiceCreatedDispatchHandler(
            _dispatcher, Substitute.For<ILogger<InvoiceCreatedDispatchHandler>>());

        var e = new InvoiceGeneratedEvent(Guid.NewGuid(), _tenantId, "INV-0042", Guid.NewGuid());
        await handler.Handle(new DomainEventNotification<InvoiceGeneratedEvent>(e), CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            _tenantId, WebhookEventTypes.InvoiceCreated, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── invoice.sent ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvoiceSent_CallsDispatcherWithCorrectEventType()
    {
        var handler = new InvoiceSentDispatchHandler(
            _dispatcher, Substitute.For<ILogger<InvoiceSentDispatchHandler>>());

        var e = new InvoiceSentEvent(Guid.NewGuid(), _tenantId, "INV-0043", Guid.NewGuid());
        await handler.Handle(new DomainEventNotification<InvoiceSentEvent>(e), CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            _tenantId, WebhookEventTypes.InvoiceSent, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── user.invited ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UserInvited_CallsDispatcherWithCorrectEventType()
    {
        var handler = new UserInvitedDispatchHandler(
            _dispatcher, Substitute.For<ILogger<UserInvitedDispatchHandler>>());

        var e = new UserCreatedEvent(Guid.NewGuid(), _tenantId, "newuser@acme.com");
        await handler.Handle(new DomainEventNotification<UserCreatedEvent>(e), CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            _tenantId, WebhookEventTypes.UserInvited, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserInvited_DispatcherThrows_DoesNotPropagate()
    {
        _dispatcher.DispatchAsync(default, default!, default!, default)
            .ReturnsForAnyArgs(_ => throw new HttpRequestException("network error"));

        var handler = new UserInvitedDispatchHandler(
            _dispatcher, Substitute.For<ILogger<UserInvitedDispatchHandler>>());

        var e = new UserCreatedEvent(Guid.NewGuid(), _tenantId, "user@test.com");
        var act = () => handler.Handle(new DomainEventNotification<UserCreatedEvent>(e), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
