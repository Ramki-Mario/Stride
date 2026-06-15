using System.Text.Json.Serialization;

namespace STRIDE.Modules.KitOps.API.Models;

public sealed record CreateKitReservationRequest(
    [property: JsonRequired] Guid KitItemId,
    string? Notes);
