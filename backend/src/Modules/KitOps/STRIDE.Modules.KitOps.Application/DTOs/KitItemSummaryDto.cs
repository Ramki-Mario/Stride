namespace STRIDE.Modules.KitOps.Application.DTOs;

public sealed record KitItemSummaryDto(
    Guid      Id,
    string    Name,
    string    Category,
    string?   Description,
    int       TotalQuantity,
    bool      IsActive,
    DateTime  CreatedAt);
