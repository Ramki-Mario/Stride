using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Commands.InviteUser;

/// <summary>
/// Creates a pending user account within the caller's tenant.
/// The invited user starts with IsActive=false, IsPending=true and cannot log
/// in until their account is activated.
/// </summary>
public sealed record InviteUserCommand(
    Guid TenantId,
    string Email,
    string DisplayName,
    string Role,
    Guid InvitedBy) : IRequest<Result<Guid>>;
