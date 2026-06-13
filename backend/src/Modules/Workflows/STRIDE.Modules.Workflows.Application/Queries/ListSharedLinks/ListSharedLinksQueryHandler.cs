using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;

namespace STRIDE.Modules.Workflows.Application.Queries.ListSharedLinks;

internal sealed class ListSharedLinksQueryHandler
    : IRequestHandler<ListSharedLinksQuery, Result<IReadOnlyList<SharedLinkDto>>>
{
    private readonly ISharedWorkflowLinkRepository _links;
    private readonly ISharedLinkUrlBuilder         _urlBuilder;

    public ListSharedLinksQueryHandler(
        ISharedWorkflowLinkRepository links,
        ISharedLinkUrlBuilder         urlBuilder)
    {
        _links      = links;
        _urlBuilder = urlBuilder;
    }

    public async Task<Result<IReadOnlyList<SharedLinkDto>>> Handle(
        ListSharedLinksQuery request,
        CancellationToken cancellationToken)
    {
        var links = await _links.ListActiveByInstanceAsync(request.WorkflowInstanceId, cancellationToken);

        var dtos = links
            .Select(l => SharedLinkMapper.ToDto(l, _urlBuilder))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<SharedLinkDto>>(dtos);
    }
}
