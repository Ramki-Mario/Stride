using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.InviteUser;

internal sealed class InviteUserCommandHandler
    : IRequestHandler<InviteUserCommand, Result<Guid>>
{
    private readonly IAdminWriteService _writeService;

    public InviteUserCommandHandler(IAdminWriteService writeService)
        => _writeService = writeService;

    public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken ct)
    {
        var userId = await _writeService.InviteUserAsync(
            request.TenantId,
            request.Email,
            request.DisplayName,
            request.Role,
            request.InvitedBy,
            ct);

        // TODO (Phase 6 follow-up): dispatch a cross-module CreateNotificationCommand
        // to notify the invitee via in-app notification when email delivery is wired up.

        return Result.Success(userId);
    }
}
