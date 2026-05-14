namespace STRIDE.BuildingBlocks.Domain.Entities;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
}
