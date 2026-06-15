namespace STRIDE.Modules.KitOps.Application.DTOs;

public sealed record KitUsageSummaryRowDto(
    Guid    KitItemId,
    string  Name,
    string  Category,
    int     TotalQuantity,
    int     TotalCheckouts,
    int     ActiveCheckouts,
    int     OverdueCheckouts,
    int     PendingReservations,
    double? AvgDaysCheckedOut);
