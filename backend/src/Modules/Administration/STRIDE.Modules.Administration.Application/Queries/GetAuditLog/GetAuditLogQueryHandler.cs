using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Queries.GetAuditLog;

internal sealed class GetAuditLogQueryHandler
    : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    private readonly IAuditLogReadService _readService;

    public GetAuditLogQueryHandler(IAuditLogReadService readService)
        => _readService = readService;

    public Task<PagedResult<AuditLogEntryDto>> Handle(
        GetAuditLogQuery request, CancellationToken cancellationToken)
        => _readService.GetAuditLogAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            request.From,
            request.To,
            request.Action,
            cancellationToken);
}
