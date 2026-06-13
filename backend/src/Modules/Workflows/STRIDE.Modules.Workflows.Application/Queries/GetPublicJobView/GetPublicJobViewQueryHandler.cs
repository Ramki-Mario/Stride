using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Helpers;

namespace STRIDE.Modules.Workflows.Application.Queries.GetPublicJobView;

internal sealed class GetPublicJobViewQueryHandler
    : IRequestHandler<GetPublicJobViewQuery, Result<PublicJobViewDto>>
{
    private const string GoneError = "This link has expired or been revoked.";

    private readonly ISharedWorkflowLinkRepository          _links;
    private readonly IWorkflowInstanceRepository            _instances;
    private readonly ILogger<GetPublicJobViewQueryHandler>  _logger;

    public GetPublicJobViewQueryHandler(
        ISharedWorkflowLinkRepository         links,
        IWorkflowInstanceRepository           instances,
        ILogger<GetPublicJobViewQueryHandler> logger)
    {
        _links     = links;
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result<PublicJobViewDto>> Handle(
        GetPublicJobViewQuery request,
        CancellationToken     cancellationToken)
    {
        var link = await _links.GetByTokenAsync(request.Token, cancellationToken);

        // Return the same error for missing, expired, and revoked — prevents enumeration.
        if (link is null || !link.IsValid(DateTime.UtcNow))
        {
            _logger.LogInformation(
                "Public job view: token resolved to {State}",
                link is null ? "not found" : (link.IsRevoked ? "revoked" : "expired"));
            return Result.Failure<PublicJobViewDto>(GoneError);
        }

        var instance = await _instances.GetByIdPublicAsync(
            link.WorkflowInstanceId, link.TenantId, cancellationToken);

        if (instance is null)
        {
            _logger.LogWarning(
                "Public job view: link {LinkId} points to missing instance {InstanceId}",
                link.Id, link.WorkflowInstanceId);
            return Result.Failure<PublicJobViewDto>(GoneError);
        }

        link.IncrementViewCount();
        _links.Update(link);
        await _links.SaveChangesAsync(cancellationToken);

        var slaStatus = SlaStatusComputer.Compute(
            instance.CreatedAt, instance.DeadlineAt, instance.CompletedAt);

        var dto = new PublicJobViewDto(
            WorkflowName: instance.WorkflowName,
            Status:       instance.Status.ToString(),
            StartedAt:    instance.CreatedAt,
            CompletedAt:  instance.CompletedAt,
            DeadlineAt:   instance.DeadlineAt,
            SlaStatus:    slaStatus?.ToString(),
            Steps: instance.Steps
                .OrderBy(s => s.Order)
                .Select(s => new PublicStepDto(
                    StepName:    s.StepName,
                    Order:       s.Order,
                    IsRequired:  s.IsRequired,
                    Status:      s.Status.ToString(),
                    CompletedAt: s.CompletedAt,
                    DueAt:       s.DueAt))
                .ToList()
                .AsReadOnly());

        return Result.Success(dto);
    }
}
