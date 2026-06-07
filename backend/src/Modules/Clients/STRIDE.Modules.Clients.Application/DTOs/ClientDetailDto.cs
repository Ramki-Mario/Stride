namespace STRIDE.Modules.Clients.Application.DTOs;

public sealed record ClientDetailDto(
    Guid     Id,
    string   Name,
    string?  ContactPerson,
    string?  Email,
    string?  Phone,
    string?  Address,
    string?  Notes,
    int      Status,
    string   StatusLabel,
    DateTime CreatedAt,
    DateTime UpdatedAt);
