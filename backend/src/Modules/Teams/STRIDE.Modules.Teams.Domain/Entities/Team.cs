using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Teams.Domain.Enums;
using STRIDE.Modules.Teams.Domain.Events;
using STRIDE.Modules.Teams.Domain.Exceptions;

namespace STRIDE.Modules.Teams.Domain.Entities;

/// <summary>
/// Input record for creating a new team.
/// </summary>
public sealed record NewTeam(
    Guid    TenantId,
    string  Name,
    string? Description,
    Guid?   ParentTeamId,
    Guid    CreatedBy);

/// <summary>
/// A department or sub-department grouping within a tenant.
/// Supports self-referential hierarchy via <see cref="ParentTeamId"/>.
/// </summary>
public sealed class Team : AuditableEntity
{
    public const int NameMaxLength        = 100;
    public const int DescriptionMaxLength = 500;

    public string     Name         { get; private set; } = string.Empty;
    public string?    Description  { get; private set; }
    public Guid?      ParentTeamId { get; private set; }
    public TeamStatus Status       { get; private set; }

    private Team() { }

    /// <summary>Creates and returns a new team. Raises <see cref="TeamCreatedEvent"/>.</summary>
    public static Team Create(NewTeam input)
    {
        ValidateName(input.Name);
        ValidateDescription(input.Description);

        var now = DateTime.UtcNow;
        var team = new Team
        {
            Id           = Guid.NewGuid(),
            TenantId     = input.TenantId,
            Name         = input.Name.Trim(),
            Description  = input.Description?.Trim(),
            ParentTeamId = input.ParentTeamId,
            Status       = TeamStatus.Active,
            CreatedAt    = now,
            UpdatedAt    = now,
            CreatedBy    = input.CreatedBy,
        };

        team.RaiseDomainEvent(new TeamCreatedEvent(
            team.Id, team.TenantId, team.Name, input.CreatedBy));

        return team;
    }

    /// <summary>Updates name, description, and parent. Raises <see cref="TeamUpdatedEvent"/>.</summary>
    public void Update(string name, string? description, Guid? parentTeamId, Guid updatedBy)
    {
        if (IsDeleted)
            throw new TeamDomainException("A deleted team cannot be updated.");

        ValidateName(name);
        ValidateDescription(description);

        Name         = name.Trim();
        Description  = description?.Trim();
        ParentTeamId = parentTeamId;
        UpdatedAt    = DateTime.UtcNow;

        RaiseDomainEvent(new TeamUpdatedEvent(Id, TenantId, Name, updatedBy));
    }

    /// <summary>Sets status to Inactive. Raises <see cref="TeamDeactivatedEvent"/>.</summary>
    public void Deactivate(Guid deactivatedBy)
    {
        if (IsDeleted)
            throw new TeamDomainException("A deleted team cannot be deactivated.");

        if (Status == TeamStatus.Inactive)
            throw new TeamDomainException("Team is already inactive.");

        Status    = TeamStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TeamDeactivatedEvent(Id, TenantId, deactivatedBy));
    }

    /// <summary>Sets status back to Active. Raises <see cref="TeamReactivatedEvent"/>.</summary>
    public void Reactivate(Guid reactivatedBy)
    {
        if (IsDeleted)
            throw new TeamDomainException("A deleted team cannot be reactivated.");

        if (Status == TeamStatus.Active)
            throw new TeamDomainException("Team is already active.");

        Status    = TeamStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TeamReactivatedEvent(Id, TenantId, reactivatedBy));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new TeamDomainException("Team name cannot be empty.");

        if (name.Trim().Length > NameMaxLength)
            throw new TeamDomainException($"Team name cannot exceed {NameMaxLength} characters.");
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Trim().Length > DescriptionMaxLength)
            throw new TeamDomainException($"Team description cannot exceed {DescriptionMaxLength} characters.");
    }
}
