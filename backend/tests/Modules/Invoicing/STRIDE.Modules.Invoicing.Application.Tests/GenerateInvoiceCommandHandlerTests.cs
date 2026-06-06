using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Invoicing.Application.Commands.GenerateInvoice;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Tests;

public sealed class GenerateInvoiceCommandHandlerTests
{
    private readonly IInvoiceRepository _repo        = Substitute.For<IInvoiceRepository>();
    private readonly IAuditLogger       _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();

    private readonly GenerateInvoiceCommandHandler _sut;

    private static readonly DateOnly FutureDueDate =
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

    public GenerateInvoiceCommandHandlerTests()
    {
        _currentUser.Email.Returns("actor@company.com");
        _sut = new GenerateInvoiceCommandHandler(_repo, _audit, _currentUser);
    }

    [Fact]
    public async Task Handle_WithUniqueInvoiceNumber_ReturnsSuccessWithInvoiceId()
    {
        // Arrange
        _repo.GetByNumberAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        var command = BuildCommand("INV-001");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithUniqueInvoiceNumber_PersistsInvoiceToRepository()
    {
        // Arrange
        _repo.GetByNumberAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        // Act
        await _sut.Handle(BuildCommand("INV-001"), CancellationToken.None);

        // Assert
        await _repo.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateInvoiceNumber_ReturnsFailure()
    {
        // Arrange — simulate an existing invoice with the same number
        var existingInvoice = BuildExistingInvoice("INV-001");
        _repo.GetByNumberAsync(Arg.Any<Guid>(), "INV-001", Arg.Any<CancellationToken>())
            .Returns(existingInvoice);

        var command = BuildCommand("INV-001");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WithDuplicateInvoiceNumber_DoesNotPersistAnything()
    {
        // Arrange
        var existingInvoice = BuildExistingInvoice("INV-001");
        _repo.GetByNumberAsync(Arg.Any<Guid>(), "INV-001", Arg.Any<CancellationToken>())
            .Returns(existingInvoice);

        // Act
        await _sut.Handle(BuildCommand("INV-001"), CancellationToken.None);

        // Assert
        await _repo.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_CallsAuditLogger()
    {
        // Arrange
        _repo.GetByNumberAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        // Act
        await _sut.Handle(BuildCommand("INV-001"), CancellationToken.None);

        // Assert
        await _audit.Received(1).LogAsync(
            Arg.Is<AuditLogEntry>(e =>
                e.Action       == AuditActions.InvoiceGenerated &&
                e.ResourceType == "Invoice"));
    }

    [Fact]
    public async Task Handle_OnDuplicateNumber_DoesNotCallAuditLogger()
    {
        // Arrange
        _repo.GetByNumberAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildExistingInvoice("INV-001"));

        // Act
        await _sut.Handle(BuildCommand("INV-001"), CancellationToken.None);

        // Assert — audit must not fire on failed commands
        await _audit.DidNotReceive().LogAsync(Arg.Any<AuditLogEntry>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GenerateInvoiceCommand BuildCommand(string invoiceNumber) =>
        new(
            TenantId:      Guid.NewGuid(),
            InvoiceNumber: invoiceNumber,
            ClientName:    "Acme Corp",
            ClientEmail:   "billing@acme.com",
            Currency:      "USD",
            DueDate:       FutureDueDate,
            Notes:         null,
            LineItems:     new[] { new GenerateInvoiceLineItem("Consulting", 500m, 2) },
            CreatedBy:     Guid.NewGuid());

    private static Invoice BuildExistingInvoice(string number)
    {
        var inv = Invoice.Generate(new NewInvoice(
            TenantId:      Guid.NewGuid(),
            InvoiceNumber: number,
            ClientName:    "Acme",
            ClientEmail:   "billing@acme.com",
            Currency:      "USD",
            DueDate:       FutureDueDate,
            CreatedBy:     Guid.NewGuid()));
        inv.ClearDomainEvents();
        return inv;
    }
}
