namespace STRIDE.Modules.Workflows.API.Dtos;

/// <summary>Body for POST /api/workflows/instances/{id}/share. ExpiryDays is optional (defaults to 30).</summary>
public sealed record CreateSharedLinkRequest(int? ExpiryDays = null);
