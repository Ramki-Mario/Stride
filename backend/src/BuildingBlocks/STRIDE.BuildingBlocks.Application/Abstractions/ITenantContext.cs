namespace STRIDE.BuildingBlocks.Application.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
}
