using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;

internal sealed class CreateSharedLinkCommandHandler
    : IRequestHandler<CreateSharedLinkCommand, Result<SharedLinkDto>>
{
    private readonly ISharedWorkflowLinkRepository _links;
    private readonly IWorkflowInstanceRepository   _instances;
    private readonly ISharedLinkUrlBuilder         _urlBuilder;

    public CreateSharedLinkCommandHandler(
        ISharedWorkflowLinkRepository links,
        IWorkflowInstanceRepository   instances,
        ISharedLinkUrlBuilder         urlBuilder)
    {
        _links      = links;
        _instances  = instances;
        _urlBuilder = urlBuilder;
    }

    public async Task<Result<SharedLinkDto>> Handle(
        CreateSharedLinkCommand request,
        CancellationToken cancellationToken)
    {
        // Confirm the instance exists and belongs to this tenant before minting a link.
        var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<SharedLinkDto>(
                $"Workflow instance '{request.WorkflowInstanceId}' not found.");

        SharedWorkflowLink link;
        try
        {
            link = SharedWorkflowLink.Create(
                request.WorkflowInstanceId,
                request.TenantId,
                request.CreatedByUserId,
                request.ExpiryDays ?? SharedWorkflowLink.DefaultExpiryDays);
        }
        catch (WorkflowDomainException ex)
        {
            return Result.Failure<SharedLinkDto>(ex.Message);
        }

        await _links.AddAsync(link, cancellationToken);
        await _links.SaveChangesAsync(cancellationToken);

        return Result.Success(SharedLinkMapper.ToDto(link, _urlBuilder));
    }
}
