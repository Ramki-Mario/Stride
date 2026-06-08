namespace STRIDE.Modules.Teams.Application.DTOs;

public sealed record TeamSummaryDto(
    Guid      Id,
    string    Name,
    string?   Description,
    Guid?     ParentTeamId,
    string?   ParentTeamName,
    int       Status,
    string    StatusLabel,
    DateTime  CreatedAt);
