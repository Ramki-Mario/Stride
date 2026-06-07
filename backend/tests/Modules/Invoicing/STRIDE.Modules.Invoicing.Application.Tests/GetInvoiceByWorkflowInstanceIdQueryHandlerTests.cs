using STRIDE.Modules.Invoicing.Application.DTOs;
using STRIDE.Modules.Invoicing.Application.Queries.GetInvoiceByWorkflowInstanceId;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Tests;

public sealed class GetInvoiceByWorkflowInstanceIdQueryHandlerTests
{
    private readonly IInvoiceRepository _repo =
        Substitute.For<IInvoiceRepository>();

    private readonly GetInvoiceByWorkflowInstanceIdQueryHandler _sut;

    private static readonly DateOnly FutureDueDate =
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

    public GetInvoiceByWorkflowInstanceIdQueryHandlerTests()
    {
        _sut = new GetInvoiceByWorkflowInstanceIdQueryHandler(_repo);
    }

    [Fact]
    public async Task Handle_WhenNoInvoiceExists_ReturnsSuccessWithNullValue()
    {
        // Arrange
        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        var query = new GetInvoiceByWorkflowInstanceIdQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenInvoiceExists_ReturnsSuccessWithReferenceDto()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var invoice = BuildInvoice("WF-ABCD1234");
        invoice.ClearDomainEvents();

        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), workflowInstanceId, Arg.Any<CancellationToken>())
            .Returns(invoice);

        var query = new GetInvoiceByWorkflowInstanceIdQuery(Guid.NewGuid(), workflowInstanceId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.InvoiceNumber.Should().Be("WF-ABCD1234");
        result.Value.Id.Should().Be(invoice.Id);
    }

    [Fact]
    public async Task Handle_WhenInvoiceExists_StatusLabelMatchesDraftStatus()
    {
        // Arrange
        var invoice = BuildInvoice("WF-DRAFT001");
        invoice.ClearDomainEvents();

        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(invoice);

        var query = new GetInvoiceByWorkflowInstanceIdQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Value!.Status.Should().Be(0);        // Draft = 0
        result.Value.StatusLabel.Should().Be("Draft");
    }

    [Fact]
    public async Task Handle_UsesCorrectTenantIdAndWorkflowInstanceId()
    {
        // Arrange
        var tenantId           = Guid.NewGuid();
        var workflowInstanceId = Guid.NewGuid();

        _repo.GetBySourceWorkflowInstanceIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        var query = new GetInvoiceByWorkflowInstanceIdQuery(tenantId, workflowInstanceId);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert — repo called with exact IDs
        await _repo.Received(1).GetBySourceWorkflowInstanceIdAsync(
            tenantId, workflowInstanceId, Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Invoice BuildInvoice(string invoiceNumber) =>
        Invoice.Generate(new NewInvoice(
            TenantId:      Guid.NewGuid(),
            InvoiceNumber: invoiceNumber,
            ClientName:    "Test Client",
            ClientEmail:   null,
            Currency:      "GBP",
            DueDate:       FutureDueDate,
            CreatedBy:     Guid.NewGuid()));
}
