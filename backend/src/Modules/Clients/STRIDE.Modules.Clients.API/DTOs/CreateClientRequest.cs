namespace STRIDE.Modules.Clients.API.DTOs;

public sealed record CreateClientRequest(
    string  Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes);
