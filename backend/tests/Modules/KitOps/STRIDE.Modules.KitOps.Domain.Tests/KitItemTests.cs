using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Events;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Tests.Builders;

namespace STRIDE.Modules.KitOps.Domain.Tests;

public sealed class KitItemTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_ReturnsActiveItem()
    {
        var item = new KitItemBuilder().Build();

        item.IsActive.Should().BeTrue();
        item.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ValidInput_RaisesKitItemCreatedEvent()
    {
        var item = new KitItemBuilder().Build();

        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitItemCreatedEvent>();
    }

    [Fact]
    public void Create_TrimsName()
    {
        var item = new KitItemBuilder().WithName("  Safety Helmet  ").Build();

        item.Name.Should().Be("Safety Helmet");
    }

    [Fact]
    public void Create_TrimsCategory()
    {
        var item = new KitItemBuilder().WithCategory("  PPE  ").Build();

        item.Category.Should().Be("PPE");
    }

    [Fact]
    public void Create_SetsAllCoreProperties()
    {
        var tenantId = Guid.NewGuid();
        var item = KitItem.Create(new NewKitItem(tenantId, "Helmet", "PPE", "Hard hat", 3, Guid.NewGuid()));

        item.TenantId.Should().Be(tenantId);
        item.Name.Should().Be("Helmet");
        item.Category.Should().Be("PPE");
        item.Description.Should().Be("Hard hat");
        item.TotalQuantity.Should().Be(3);
    }

    [Fact]
    public void Create_EmptyName_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder().WithName("").Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_WhitespaceName_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder().WithName("   ").Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_NameTooLong_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder().WithName(new string('A', KitItem.NameMaxLength + 1)).Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage($"*{KitItem.NameMaxLength}*");
    }

    [Fact]
    public void Create_EmptyCategory_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder().WithCategory("").Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_CategoryTooLong_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder().WithCategory(new string('X', KitItem.CategoryMaxLength + 1)).Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage($"*{KitItem.CategoryMaxLength}*");
    }

    [Fact]
    public void Create_DescriptionTooLong_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder()
            .WithDescription(new string('x', KitItem.DescriptionMaxLength + 1)).Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage($"*{KitItem.DescriptionMaxLength}*");
    }

    [Fact]
    public void Create_NullDescription_IsAccepted()
    {
        var item = new KitItemBuilder().WithDescription(null).Build();

        item.Description.Should().BeNull();
    }

    [Fact]
    public void Create_ZeroQuantity_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder().WithTotalQuantity(0).Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_NegativeQuantity_ThrowsKitOpsDomainException()
    {
        var act = () => new KitItemBuilder().WithTotalQuantity(-1).Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage("*greater than zero*");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ValidInput_UpdatesProperties()
    {
        var item = new KitItemBuilder().Build();

        item.Update("Gloves", "PPE", "Work gloves", 10, Guid.NewGuid());

        item.Name.Should().Be("Gloves");
        item.Category.Should().Be("PPE");
        item.Description.Should().Be("Work gloves");
        item.TotalQuantity.Should().Be(10);
    }

    [Fact]
    public void Update_ValidInput_RaisesKitItemUpdatedEvent()
    {
        var item = new KitItemBuilder().Build();
        item.ClearDomainEvents();

        item.Update("Gloves", "PPE", null, 2, Guid.NewGuid());

        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitItemUpdatedEvent>();
    }

    [Fact]
    public void Update_ZeroQuantity_ThrowsKitOpsDomainException()
    {
        var item = new KitItemBuilder().Build();

        var act = () => item.Update("Helmet", "PPE", null, 0, Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ActiveItem_SetsIsActiveFalse()
    {
        var item = new KitItemBuilder().Build();

        item.Deactivate(Guid.NewGuid());

        item.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ActiveItem_RaisesKitItemDeactivatedEvent()
    {
        var item = new KitItemBuilder().Build();
        item.ClearDomainEvents();

        item.Deactivate(Guid.NewGuid());

        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitItemDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsKitOpsDomainException()
    {
        var item = new KitItemBuilder().Build();
        item.Deactivate(Guid.NewGuid());

        var act = () => item.Deactivate(Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*already inactive*");
    }

    // ── Reactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Reactivate_InactiveItem_SetsIsActiveTrue()
    {
        var item = new KitItemBuilder().Build();
        item.Deactivate(Guid.NewGuid());

        item.Reactivate(Guid.NewGuid());

        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_InactiveItem_RaisesKitItemReactivatedEvent()
    {
        var item = new KitItemBuilder().Build();
        item.Deactivate(Guid.NewGuid());
        item.ClearDomainEvents();

        item.Reactivate(Guid.NewGuid());

        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitItemReactivatedEvent>();
    }

    [Fact]
    public void Reactivate_AlreadyActive_ThrowsKitOpsDomainException()
    {
        var item = new KitItemBuilder().Build();

        var act = () => item.Reactivate(Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*already active*");
    }

    // ── Deleted-guard paths ───────────────────────────────────────────────────

    [Fact]
    public void Update_DeletedItem_ThrowsKitOpsDomainException()
    {
        var item = new KitItemBuilder().Build();
        MarkDeleted(item);

        var act = () => item.Update("x", "y", null, 1, Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Deactivate_DeletedItem_ThrowsKitOpsDomainException()
    {
        var item = new KitItemBuilder().Build();
        MarkDeleted(item);

        var act = () => item.Deactivate(Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Reactivate_DeletedItem_ThrowsKitOpsDomainException()
    {
        var item = new KitItemBuilder().Build();
        item.Deactivate(Guid.NewGuid());
        MarkDeleted(item);

        var act = () => item.Reactivate(Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*deleted*");
    }

    private static void MarkDeleted(KitItem item)
        => item.GetType().GetProperty("IsDeleted")!.SetValue(item, true);
}
