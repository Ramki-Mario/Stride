namespace STRIDE.Modules.KitOps.API.Models;

public sealed record CreateKitItemRequest(
    string  Name,
    string  Category,
    string? Description,
    int     TotalQuantity);
