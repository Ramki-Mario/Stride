using STRIDE.Modules.Clients.Domain.Entities;

namespace STRIDE.Modules.Clients.Domain.Tests.Builders;

/// <summary>
/// Test builder for creating Client aggregates in a known valid state.
/// </summary>
public sealed class ClientBuilder
{
    private Guid    _tenantId      = Guid.NewGuid();
    private Guid    _createdBy     = Guid.NewGuid();
    private string  _name          = "Acme Corporation";
    private string? _contactPerson = "John Smith";
    private string? _email         = "billing@acme.com";
    private string? _phone         = "+44 7700 900000";
    private string? _address       = "1 Business Park, London, UK";
    private string? _notes         = "Preferred client.";

    public ClientBuilder WithName(string name)              { _name = name;              return this; }
    public ClientBuilder WithEmail(string? email)           { _email = email;            return this; }
    public ClientBuilder WithTenantId(Guid tenantId)        { _tenantId = tenantId;      return this; }
    public ClientBuilder WithContactPerson(string? contact) { _contactPerson = contact;  return this; }
    public ClientBuilder WithNoOptionalFields()
    {
        _contactPerson = null;
        _email         = null;
        _phone         = null;
        _address       = null;
        _notes         = null;
        return this;
    }

    public Client Build() => Client.Create(new NewClient(
        TenantId:      _tenantId,
        Name:          _name,
        ContactPerson: _contactPerson,
        Email:         _email,
        Phone:         _phone,
        Address:       _address,
        Notes:         _notes,
        CreatedBy:     _createdBy));
}
