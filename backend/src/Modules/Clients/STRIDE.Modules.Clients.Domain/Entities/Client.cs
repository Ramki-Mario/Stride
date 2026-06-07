using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Clients.Domain.Enums;
using STRIDE.Modules.Clients.Domain.Events;
using STRIDE.Modules.Clients.Domain.Exceptions;

namespace STRIDE.Modules.Clients.Domain.Entities;

public sealed record NewClient(
    Guid    TenantId,
    string  Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    Guid    CreatedBy);

/// <summary>
/// Client aggregate root.
/// Represents an external customer or organisation that workflows and invoices
/// are performed/issued for within a tenant.
/// </summary>
public sealed class Client : AuditableEntity
{
    public string       Name          { get; private set; } = string.Empty;
    public string?      ContactPerson { get; private set; }
    public string?      Email         { get; private set; }
    public string?      Phone         { get; private set; }
    public string?      Address       { get; private set; }
    public string?      Notes         { get; private set; }
    public ClientStatus Status        { get; private set; }

    // EF Core constructor
    private Client() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Client Create(NewClient data)
    {
        if (string.IsNullOrWhiteSpace(data.Name))
            throw new ClientDomainException("Client name is required.");

        if (data.Email is not null && !data.Email.Contains('@'))
            throw new ClientDomainException("Client email is not valid.");

        var client = new Client
        {
            Id            = Guid.NewGuid(),
            TenantId      = data.TenantId,
            Name          = data.Name.Trim(),
            ContactPerson = data.ContactPerson?.Trim(),
            Email         = data.Email?.Trim().ToLowerInvariant(),
            Phone         = data.Phone?.Trim(),
            Address       = data.Address?.Trim(),
            Notes         = data.Notes?.Trim(),
            Status        = ClientStatus.Active,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow,
            CreatedBy     = data.CreatedBy,
        };

        client.RaiseDomainEvent(new ClientCreatedEvent(client.Id, data.TenantId, client.Name, data.CreatedBy));

        return client;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public void Update(
        string  name,
        string? contactPerson,
        string? email,
        string? phone,
        string? address,
        string? notes,
        Guid    updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ClientDomainException("Client name is required.");

        if (email is not null && !email.Contains('@'))
            throw new ClientDomainException("Client email is not valid.");

        Name          = name.Trim();
        ContactPerson = contactPerson?.Trim();
        Email         = email?.Trim().ToLowerInvariant();
        Phone         = phone?.Trim();
        Address       = address?.Trim();
        Notes         = notes?.Trim();
        UpdatedAt     = DateTime.UtcNow;

        RaiseDomainEvent(new ClientUpdatedEvent(Id, TenantId, updatedBy));
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    public void Deactivate(Guid deactivatedBy)
    {
        if (Status == ClientStatus.Inactive)
            throw new ClientDomainException("Client is already inactive.");

        Status    = ClientStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ClientDeactivatedEvent(Id, TenantId, deactivatedBy));
    }

    // ── Reactivate ────────────────────────────────────────────────────────────

    public void Reactivate(Guid reactivatedBy)
    {
        if (Status == ClientStatus.Active)
            throw new ClientDomainException("Client is already active.");

        Status    = ClientStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ClientUpdatedEvent(Id, TenantId, reactivatedBy));
    }
}
