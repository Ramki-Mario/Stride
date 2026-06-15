namespace STRIDE.Modules.KitOps.Application.DTOs;

/// <summary>
/// Live availability snapshot for a kit item.
/// <c>AvailableQuantity = max(0, TotalQuantity - OutstandingCheckouts)</c> — never negative.
/// </summary>
public sealed record KitItemAvailabilityDto(
    Guid   KitItemId,
    string Name,
    int    TotalQuantity,
    int    OutstandingCheckouts,
    int    AvailableQuantity,
    int    OverdueCheckouts);
