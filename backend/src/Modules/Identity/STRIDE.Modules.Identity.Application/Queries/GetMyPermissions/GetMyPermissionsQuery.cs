using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Queries.GetMyPermissions;

/// <summary>
/// Returns the effective permission keys held by the currently authenticated user
/// within their tenant. Used by the SPA to drive permission-aware UI (hide/disable
/// actions the user cannot perform).
/// </summary>
public sealed record GetMyPermissionsQuery() : IRequest<Result<IReadOnlyList<string>>>;
