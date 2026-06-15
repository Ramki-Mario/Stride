using System.Text.Json.Serialization;

namespace STRIDE.Modules.KitOps.API.Models;

public sealed record CheckoutKitItemRequest(
    [property: JsonRequired] Guid KitItemId,
    [property: JsonRequired] int  Days,
    string? Notes);
