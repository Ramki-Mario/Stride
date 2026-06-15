namespace STRIDE.Modules.KitOps.API.Models;

public sealed record UpdateKitItemRequest(
    string  Name,
    string  Category,
    string? Description,
    int     TotalQuantity);
