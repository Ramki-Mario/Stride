using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Invoicing.Application.EventHandlers;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Repositories;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Invoicing.Application.Tests;

public sealed class CreateInvoiceDraftFromWorkflowHandlerTests
{
    private readonly IInvoiceRepository _repo =
        Substitute.For<IInvoiceRepository>();

    private readonly CreateInvoiceDraftFromWorkflowHandler _sut;

    public CreateInvoiceDraftFromWorkflowHandlerTests()
    {
        _sut = new CreateInvoiceDraftFromWorkflowHandler(
            _repo,
            NullLogger<CreateInvoiceDraftFromWorkflowHandler>.Instance);
    }

    // ── Happy path — no existing draft ───────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoDraftExists_CreatesAndPersistsDraftInvoice()
    {
        // Arrange
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        var e = BuildEvent();

        // Act
        await _sut.Handle(Wrap(e), CancellationToken.None);

        // Assert
        await _repo.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoDraftExists_InvoiceNumberUsesWorkflowInstanceIdPrefix()
    {
        // Arrange
        Invoice? captured = null;
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repo.When(r => r.AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()))
             .Do(ci => captured = ci.Arg<Invoice>());

        var instanceId = Guid.NewGuid();
        var e = BuildEvent(workflowInstanceId: instanceId);

        // Act
        await _sut.Handle(Wrap(e), CancellationToken.None);

        // Assert
        var expectedNumber = $"WF-{instanceId.ToString("N")[..8].ToUpperInvariant()}";
        captured.Should().NotBeNull();
        captured!.InvoiceNumber.Should().Be(expectedNumber);
    }

    [Fact]
    public async Task Handle_WhenNoDraftExists_SetsSourceWorkflowInstanceId()
    {
        // Arrange
        Invoice? captured = null;
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repo.When(r => r.AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()))
             .Do(ci => captured = ci.Arg<Invoice>());

        var instanceId = Guid.NewGuid();
        var e = BuildEvent(workflowInstanceId: instanceId);

        // Act
        await _sut.Handle(Wrap(e), CancellationToken.None);

        // Assert
        captured!.SourceWorkflowInstanceId.Should().Be(instanceId);
    }

    [Fact]
    public async Task Handle_WithBillableItems_AddsLineItemsToInvoice()
    {
        // Arrange
        Invoice? captured = null;
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repo.When(r => r.AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()))
             .Do(ci => captured = ci.Arg<Invoice>());

        var items = new[]
        {
            new WorkflowBillableItemSnapshot("Labour",      2.5m,  80.00m, "Hours"),
            new WorkflowBillableItemSnapshot("Replacement", 1.0m,  45.00m, "Each"),
        };
        var e = BuildEvent(billableItems: items);

        // Act
        await _sut.Handle(Wrap(e), CancellationToken.None);

        // Assert
        captured!.LineItems.Should().HaveCount(2);
        // Each line item is mapped as qty=1, unitPrice=lineTotal
        captured.LineItems[0].UnitPrice.Should().Be(2.5m * 80.00m);
        captured.LineItems[0].Quantity.Should().Be(1);
        captured.LineItems[1].UnitPrice.Should().Be(45.00m);
    }

    [Fact]
    public async Task Handle_WithNoBillableItems_CreatesBlankDraftWithNoLineItems()
    {
        // Arrange
        Invoice? captured = null;
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repo.When(r => r.AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()))
             .Do(ci => captured = ci.Arg<Invoice>());

        var e = BuildEvent(billableItems: []); // empty items

        // Act
        await _sut.Handle(Wrap(e), CancellationToken.None);

        // Assert
        captured!.LineItems.Should().BeEmpty();
        await _repo.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithClientId_SetsClientIdOnInvoice()
    {
        // Arrange
        Invoice? captured = null;
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repo.When(r => r.AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()))
             .Do(ci => captured = ci.Arg<Invoice>());

        var clientId = Guid.NewGuid();
        var e = BuildEvent(clientId: clientId);

        // Act
        await _sut.Handle(Wrap(e), CancellationToken.None);

        // Assert
        captured!.ClientId.Should().Be(clientId);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenDraftAlreadyExists_DoesNotCreateDuplicate()
    {
        // Arrange — existing draft returned
        var existingInvoice = BuildExistingInvoice();
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(existingInvoice);

        var e = BuildEvent();

        // Act
        await _sut.Handle(Wrap(e), CancellationToken.None);

        // Assert
        await _repo.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Invoice BuildExistingInvoice() =>
        Invoice.Generate(new NewInvoice(
            TenantId:      Guid.NewGuid(),
            InvoiceNumber: "WF-EXISTING",
            ClientName:    "Existing Client",
            ClientEmail:   null,
            Currency:      "GBP",
            DueDate:       DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedBy:     Guid.NewGuid()));

    private static DomainEventNotification<WorkflowCompletedEvent> Wrap(WorkflowCompletedEvent e)
        => new(e);

    private static WorkflowCompletedEvent BuildEvent(
        Guid?   workflowInstanceId = null,
        Guid?   tenantId           = null,
        Guid?   clientId           = null,
        string  workflowName       = "Test Workflow",
        IReadOnlyList<WorkflowBillableItemSnapshot>? billableItems = null)
    {
        return new WorkflowCompletedEvent(
            WorkflowInstanceId: workflowInstanceId ?? Guid.NewGuid(),
            TenantId:           tenantId           ?? Guid.NewGuid(),
            StartedBy:          Guid.NewGuid(),
            WorkflowName:       workflowName,
            ClientId:           clientId,
            BillableItems:      billableItems ?? []);
    }
}
