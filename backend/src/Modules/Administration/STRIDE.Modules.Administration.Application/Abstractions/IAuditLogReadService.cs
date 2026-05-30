using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Queries.GetAuditLog;

namespace STRIDE.Modules.Administration.Application.Abstractions;

public interface IAuditLogReadService
{
    Task<PagedResult<AuditLogEntryDto>> GetAuditLogAsync(
        Guid      tenantId,
        int       page,
        int       pageSize,
        DateTime? from   = null,
        DateTime? to     = null,
        string?   action = null,
        CancellationToken ct = default);
}
