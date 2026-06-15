namespace STRIDE.Modules.KitOps.Application.DTOs;

public sealed record KitCheckoutReportRowDto(
    Guid     CheckoutId,
    string   KitItemName,
    string   Category,
    Guid     CheckedOutByUserId,
    string?  CheckedOutByEmail,
    DateTime CheckedOutAt,
    DateTime ExpectedReturnAt,
    DateTime? ReturnedAt,
    string   Status,
    int      DaysCheckedOut);
