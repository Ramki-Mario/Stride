namespace STRIDE.Modules.Teams.API.Models;

public sealed record UpdateTeamRequest(
    string  Name,
    string? Description,
    Guid?   ParentTeamId);
