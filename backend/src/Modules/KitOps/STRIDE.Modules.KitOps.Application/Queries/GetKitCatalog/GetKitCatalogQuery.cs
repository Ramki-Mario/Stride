using MediatR;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitCatalog;

public sealed record GetKitCatalogQuery(Guid TenantId)
    : IRequest<IReadOnlyList<KitCatalogItemDto>>;
