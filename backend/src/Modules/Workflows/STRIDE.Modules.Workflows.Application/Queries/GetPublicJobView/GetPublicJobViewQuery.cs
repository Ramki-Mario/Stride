using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.GetPublicJobView;

/// <summary>
/// Resolves a shareable link token and returns the redacted public job view.
/// No tenant context is required — the token itself is the sole credential.
/// </summary>
public sealed record GetPublicJobViewQuery(string Token)
    : IRequest<Result<PublicJobViewDto>>;
