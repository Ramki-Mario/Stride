namespace STRIDE.Modules.KitOps.API.Models;

public sealed record CheckoutKitItemRequest(
    Guid    KitItemId,
    int     Days,
    string? Notes);
