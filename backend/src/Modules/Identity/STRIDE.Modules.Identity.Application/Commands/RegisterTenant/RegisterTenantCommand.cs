using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;

namespace STRIDE.Modules.Identity.Application.Commands.RegisterTenant;

/// <summary>
/// Atomically provisions a new tenant (org + admin user) and returns a JWT so
/// the caller can be immediately signed in — same response shape as LoginCommand.
/// </summary>
public sealed record RegisterTenantCommand(
    string OrgName,
    string Slug,
    string Plan,
    string AdminEmail,
    string AdminPassword,
    string AdminDisplayName) : IRequest<Result<LoginResult>>;
