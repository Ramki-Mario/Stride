using STRIDE.Modules.Invoicing.Domain.Entities;

namespace STRIDE.Modules.Invoicing.Domain.Tests.Builders;

/// <summary>
/// Fluent builder for <see cref="Invoice"/> test fixtures.
/// Produces a valid Draft invoice by default — chain methods to customise.
/// </summary>
internal sealed class InvoiceBuilder
{
    private Guid     _tenantId     = Guid.NewGuid();
    private string   _number       = "INV-001";
    private string   _clientName   = "Acme Corp";
    private string   _clientEmail  = "billing@acme.com";
    private string   _currency     = "USD";
    private DateOnly _dueDate      = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
    private string?  _notes        = null;
    private Guid     _createdBy    = Guid.NewGuid();

    public InvoiceBuilder WithTenantId(Guid id)        { _tenantId    = id;    return this; }
    public InvoiceBuilder WithNumber(string number)    { _number      = number; return this; }
    public InvoiceBuilder WithClientName(string name)  { _clientName  = name;  return this; }
    public InvoiceBuilder WithDueDate(DateOnly date)   { _dueDate     = date;  return this; }
    public InvoiceBuilder WithCurrency(string cur)     { _currency    = cur;   return this; }

    public Invoice Build()
    {
        var invoice = Invoice.Generate(new NewInvoice(
            TenantId:      _tenantId,
            InvoiceNumber: _number,
            ClientName:    _clientName,
            ClientEmail:   _clientEmail,
            Currency:      _currency,
            DueDate:       _dueDate,
            CreatedBy:     _createdBy,
            Notes:         _notes));
        invoice.ClearDomainEvents();
        return invoice;
    }

    /// <summary>Returns a Draft invoice with one line item, events cleared.</summary>
    public static Invoice DraftWithLineItem()
    {
        var invoice = new InvoiceBuilder().Build();
        invoice.AddLineItem("Consulting services", 500m, 2);
        invoice.ClearDomainEvents();
        return invoice;
    }
}
