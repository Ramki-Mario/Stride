namespace STRIDE.Modules.Teams.API.Models;

public sealed record CreateTeamRequest(
    string  Name,
    string? Description,
    Guid?   ParentTeamId);
