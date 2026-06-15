using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Domain.Tests.Builders;

internal sealed class KitCheckoutBuilder
{
    private Guid     _tenantId           = Guid.NewGuid();
    private Guid     _kitItemId          = Guid.NewGuid();
    private Guid     _checkedOutByUserId = Guid.NewGuid();
    private DateTime _expectedReturnAt   = DateTime.UtcNow.AddDays(7);
    private string?  _notes              = null;

    public KitCheckoutBuilder WithTenantId(Guid tenantId)           { _tenantId = tenantId;                     return this; }
    public KitCheckoutBuilder WithKitItemId(Guid kitItemId)         { _kitItemId = kitItemId;                   return this; }
    public KitCheckoutBuilder WithCheckedOutByUserId(Guid userId)    { _checkedOutByUserId = userId;             return this; }
    public KitCheckoutBuilder WithExpectedReturnAt(DateTime date)    { _expectedReturnAt = date;                 return this; }
    public KitCheckoutBuilder WithNotes(string? notes)               { _notes = notes;                          return this; }

    public KitCheckout Build() =>
        KitCheckout.Create(new NewKitCheckout(
            _tenantId, _kitItemId, _checkedOutByUserId, _expectedReturnAt, _notes));
}
