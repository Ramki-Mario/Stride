namespace STRIDE.Modules.Clients.Application.DTOs;

public sealed record ClientSummaryDto(
    Guid     Id,
    string   Name,
    string?  ContactPerson,
    string?  Email,
    string?  Phone,
    int      Status,
    string   StatusLabel,
    DateTime CreatedAt);
