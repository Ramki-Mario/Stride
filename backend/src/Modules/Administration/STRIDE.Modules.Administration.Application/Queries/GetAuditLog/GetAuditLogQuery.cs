using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Queries.GetAuditLog;

public sealed record GetAuditLogQuery(
    Guid      TenantId,
    int       Page,
    int       PageSize,
    DateTime? From   = null,
    DateTime? To     = null,
    string?   Action = null)
    : IRequest<PagedResult<AuditLogEntryDto>>;
