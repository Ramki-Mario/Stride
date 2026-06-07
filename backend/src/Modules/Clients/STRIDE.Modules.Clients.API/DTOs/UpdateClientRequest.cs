namespace STRIDE.Modules.Clients.API.DTOs;

public sealed record UpdateClientRequest(
    string  Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes);
