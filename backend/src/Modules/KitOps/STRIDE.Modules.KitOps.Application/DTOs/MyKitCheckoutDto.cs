namespace STRIDE.Modules.KitOps.Application.DTOs;

public sealed record MyKitCheckoutDto(
    Guid      CheckoutId,
    Guid      KitItemId,
    string    KitItemName,
    string    Category,
    DateTime  CheckedOutAt,
    DateTime  ExpectedReturnAt,
    DateTime? ReturnedAt,
    string?   Notes,
    string    StatusLabel,
    int       StatusValue);
