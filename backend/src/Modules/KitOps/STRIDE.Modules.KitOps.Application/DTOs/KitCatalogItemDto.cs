namespace STRIDE.Modules.KitOps.Application.DTOs;

/// <summary>
/// Kit item with live availability counts — returned to field users for the browse screen.
/// </summary>
public sealed record KitCatalogItemDto(
    Guid   KitItemId,
    string Name,
    string Category,
    string? Description,
    int    TotalQuantity,
    int    OutstandingCheckouts,
    int    AvailableQuantity,
    int    OverdueCheckouts);
