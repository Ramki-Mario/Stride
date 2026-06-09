using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Domain.Tests.Builders;

/// <summary>
/// Test builder for creating Team aggregates in a known valid state.
/// </summary>
public sealed class TeamBuilder
{
    private Guid    _tenantId    = Guid.NewGuid();
    private Guid    _createdBy   = Guid.NewGuid();
    private string  _name        = "Engineering";
    private string? _description = "Core engineering team.";
    private Guid?   _parentTeamId = null;

    public TeamBuilder WithName(string name)              { _name = name;           return this; }
    public TeamBuilder WithDescription(string? desc)      { _description = desc;    return this; }
    public TeamBuilder WithParentTeamId(Guid? parentId)   { _parentTeamId = parentId; return this; }
    public TeamBuilder WithTenantId(Guid tenantId)        { _tenantId = tenantId;   return this; }
    public TeamBuilder WithCreatedBy(Guid userId)         { _createdBy = userId;    return this; }

    public Team Build() => Team.Create(new NewTeam(
        TenantId:    _tenantId,
        Name:        _name,
        Description: _description,
        ParentTeamId: _parentTeamId,
        CreatedBy:   _createdBy));
}
