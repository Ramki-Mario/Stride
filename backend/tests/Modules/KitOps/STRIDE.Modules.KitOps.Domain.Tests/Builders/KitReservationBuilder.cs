using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Domain.Tests.Builders;

internal sealed class KitReservationBuilder
{
    private Guid    _tenantId          = Guid.NewGuid();
    private Guid    _kitItemId         = Guid.NewGuid();
    private Guid    _requestedByUserId = Guid.NewGuid();
    private string? _notes             = null;

    public KitReservationBuilder WithTenantId(Guid tenantId)        { _tenantId = tenantId;           return this; }
    public KitReservationBuilder WithKitItemId(Guid kitItemId)      { _kitItemId = kitItemId;         return this; }
    public KitReservationBuilder WithRequestedByUserId(Guid userId)  { _requestedByUserId = userId;    return this; }
    public KitReservationBuilder WithNotes(string? notes)            { _notes = notes;                 return this; }

    public KitReservation Build() =>
        KitReservation.Create(new NewKitReservation(
            _tenantId, _kitItemId, _requestedByUserId, _notes));
}
