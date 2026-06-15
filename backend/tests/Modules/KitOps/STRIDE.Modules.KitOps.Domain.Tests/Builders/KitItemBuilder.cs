using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Domain.Tests.Builders;

internal sealed class KitItemBuilder
{
    private Guid    _tenantId      = Guid.NewGuid();
    private string  _name          = "Safety Helmet";
    private string  _category      = "Safety";
    private string? _description   = "Standard hard hat";
    private int     _totalQuantity = 5;
    private Guid    _createdBy     = Guid.NewGuid();

    public KitItemBuilder WithTenantId(Guid tenantId)      { _tenantId = tenantId;           return this; }
    public KitItemBuilder WithName(string name)             { _name = name;                   return this; }
    public KitItemBuilder WithCategory(string category)     { _category = category;           return this; }
    public KitItemBuilder WithDescription(string? desc)     { _description = desc;            return this; }
    public KitItemBuilder WithTotalQuantity(int qty)        { _totalQuantity = qty;           return this; }
    public KitItemBuilder WithCreatedBy(Guid userId)        { _createdBy = userId;            return this; }

    public KitItem Build() =>
        KitItem.Create(new NewKitItem(
            _tenantId, _name, _category, _description, _totalQuantity, _createdBy));
}
