namespace STRIDE.Modules.Administration.API.DTOs;

public sealed record InviteUserRequest(string Email, string DisplayName, string Role);
