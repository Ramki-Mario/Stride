using FluentAssertions;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Enums;
using STRIDE.Modules.Invoicing.Domain.Events;
using STRIDE.Modules.Invoicing.Domain.Exceptions;
using STRIDE.Modules.Invoicing.Domain.Tests.Builders;

namespace STRIDE.Modules.Invoicing.Domain.Tests;

public sealed class InvoiceTests
{
    // ── Generate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_WithValidData_ReturnsDraftInvoice()
    {
        // Arrange
        var tenantId  = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var dueDate   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        // Act
        var invoice = Invoice.Generate(new NewInvoice(
            TenantId:      tenantId,
            InvoiceNumber: "INV-001",
            ClientName:    "Acme Corp",
            ClientEmail:   "billing@acme.com",
            Currency:      "USD",
            DueDate:       dueDate,
            CreatedBy:     createdBy));

        // Assert
        invoice.Id.Should().NotBeEmpty();
        invoice.InvoiceNumber.Should().Be("INV-001");
        invoice.ClientName.Should().Be("Acme Corp");
        invoice.ClientEmail.Should().Be("billing@acme.com");
        invoice.Currency.Should().Be("USD");
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.TenantId.Should().Be(tenantId);
        invoice.TotalAmount.Should().Be(0m);
        invoice.LineItems.Should().BeEmpty();
        invoice.SentAt.Should().BeNull();
        invoice.PaidAt.Should().BeNull();
    }

    [Theory]
    [InlineData("", "clientName", "email@a.com")]
    [InlineData("INV-001", "", "email@a.com")]
    [InlineData("INV-001", "clientName", "")]
    public void Generate_WithMissingRequiredField_ThrowsInvoiceDomainException(
        string number, string clientName, string clientEmail)
    {
        // Arrange
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        // Act
        var act = () => Invoice.Generate(new NewInvoice(
            TenantId:      Guid.NewGuid(),
            InvoiceNumber: number,
            ClientName:    clientName,
            ClientEmail:   clientEmail,
            Currency:      "USD",
            DueDate:       dueDate,
            CreatedBy:     Guid.NewGuid()));

        // Assert
        act.Should().Throw<InvoiceDomainException>();
    }

    [Fact]
    public void Generate_WithPastDueDate_ThrowsInvoiceDomainException()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        // Act
        var act = () => Invoice.Generate(new NewInvoice(
            TenantId:      Guid.NewGuid(),
            InvoiceNumber: "INV-001",
            ClientName:    "Acme",
            ClientEmail:   "billing@acme.com",
            Currency:      "USD",
            DueDate:       pastDate,
            CreatedBy:     Guid.NewGuid()));

        // Assert
        act.Should().Throw<InvoiceDomainException>()
            .WithMessage("*past*");
    }

    [Fact]
    public void Generate_NormalisesClientEmailToLowercase()
    {
        // Act
        var invoice = new InvoiceBuilder().Build();

        // Assert
        invoice.ClientEmail.Should().Be(invoice.ClientEmail.ToLowerInvariant());
    }

    [Fact]
    public void Generate_RaisesInvoiceGeneratedEvent()
    {
        // Arrange
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        // Act
        var invoice = Invoice.Generate(new NewInvoice(
            TenantId:      Guid.NewGuid(),
            InvoiceNumber: "INV-001",
            ClientName:    "Acme",
            ClientEmail:   "billing@acme.com",
            Currency:      "USD",
            DueDate:       dueDate,
            CreatedBy:     Guid.NewGuid()));

        // Assert
        invoice.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<InvoiceGeneratedEvent>();
    }

    // ── AddLineItem ───────────────────────────────────────────────────────────

    [Fact]
    public void AddLineItem_WhenDraft_AddsLineItemAndUpdatesTotalAmount()
    {
        // Arrange
        var invoice = new InvoiceBuilder().Build();

        // Act
        invoice.AddLineItem("Consulting", 100m, 3);

        // Assert
        invoice.LineItems.Should().HaveCount(1);
        invoice.TotalAmount.Should().Be(300m); // 100 × 3
    }

    [Fact]
    public void AddLineItem_MultipleItems_TotalAmountSumsAll()
    {
        // Arrange
        var invoice = new InvoiceBuilder().Build();

        // Act
        invoice.AddLineItem("Item A", 200m, 2);  // 400
        invoice.AddLineItem("Item B", 50m,  3);  // 150

        // Assert
        invoice.TotalAmount.Should().Be(550m);
    }

    [Fact]
    public void AddLineItem_WhenNotDraft_ThrowsInvoiceDomainException()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();
        invoice.Send(Guid.NewGuid());
        invoice.ClearDomainEvents();

        // Act
        var act = () => invoice.AddLineItem("Late item", 10m, 1);

        // Assert
        act.Should().Throw<InvoiceDomainException>()
            .WithMessage("*Draft*");
    }

    // ── Send ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Send_WhenDraftWithLineItems_SetsSentStatus()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();

        // Act
        invoice.Send(Guid.NewGuid());

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.SentAt.Should().NotBeNull();
    }

    [Fact]
    public void Send_WhenDraftWithNoLineItems_ThrowsInvoiceDomainException()
    {
        // Arrange
        var invoice = new InvoiceBuilder().Build(); // no line items

        // Act
        var act = () => invoice.Send(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvoiceDomainException>()
            .WithMessage("*no line items*");
    }

    [Fact]
    public void Send_WhenAlreadySent_ThrowsInvoiceDomainException()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();
        invoice.Send(Guid.NewGuid());
        invoice.ClearDomainEvents();

        // Act
        var act = () => invoice.Send(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvoiceDomainException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Send_RaisesInvoiceSentEvent()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();

        // Act
        invoice.Send(Guid.NewGuid());

        // Assert
        invoice.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<InvoiceSentEvent>();
    }

    // ── MarkPaid ──────────────────────────────────────────────────────────────

    [Fact]
    public void MarkPaid_WhenSent_SetsPaidStatus()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();
        invoice.Send(Guid.NewGuid());
        invoice.ClearDomainEvents();

        // Act
        invoice.MarkPaid(Guid.NewGuid());

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkPaid_WhenDraft_ThrowsInvoiceDomainException()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();

        // Act
        var act = () => invoice.MarkPaid(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvoiceDomainException>()
            .WithMessage("*Sent*");
    }

    [Fact]
    public void MarkPaid_RaisesInvoicePaidEvent()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();
        invoice.Send(Guid.NewGuid());
        invoice.ClearDomainEvents();

        // Act
        invoice.MarkPaid(Guid.NewGuid());

        // Assert
        invoice.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<InvoicePaidEvent>();
    }

    // ── Void ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Void_WhenDraft_SetsVoidStatus()
    {
        // Arrange
        var invoice = new InvoiceBuilder().Build();

        // Act
        invoice.Void(Guid.NewGuid());

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Void);
    }

    [Fact]
    public void Void_WhenSent_SetsVoidStatus()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();
        invoice.Send(Guid.NewGuid());
        invoice.ClearDomainEvents();

        // Act
        invoice.Void(Guid.NewGuid());

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Void);
    }

    [Fact]
    public void Void_WhenPaid_ThrowsInvoiceDomainException()
    {
        // Arrange
        var invoice = InvoiceBuilder.DraftWithLineItem();
        invoice.Send(Guid.NewGuid());
        invoice.MarkPaid(Guid.NewGuid());
        invoice.ClearDomainEvents();

        // Act
        var act = () => invoice.Void(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvoiceDomainException>()
            .WithMessage("*void*Paid*");
    }

    [Fact]
    public void Void_WhenAlreadyVoided_ThrowsInvoiceDomainException()
    {
        // Arrange
        var invoice = new InvoiceBuilder().Build();
        invoice.Void(Guid.NewGuid());
        invoice.ClearDomainEvents();

        // Act
        var act = () => invoice.Void(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvoiceDomainException>()
            .WithMessage("*void*Void*");
    }
}
