namespace STRIDE.Modules.Teams.Application.DTOs;

public sealed record TeamDetailDto(
    Guid      Id,
    string    Name,
    string?   Description,
    Guid?     ParentTeamId,
    string?   ParentTeamName,
    int       Status,
    string    StatusLabel,
    DateTime  CreatedAt,
    DateTime  UpdatedAt);
