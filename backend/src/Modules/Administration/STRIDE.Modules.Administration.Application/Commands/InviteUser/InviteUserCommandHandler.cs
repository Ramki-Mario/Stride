using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.InviteUser;

internal sealed class InviteUserCommandHandler
    : IRequestHandler<InviteUserCommand, Result<Guid>>
{
    private readonly IAdminWriteService _writeService;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public InviteUserCommandHandler(
        IAdminWriteService writeService,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _writeService = writeService;
        _audit        = audit;
        _currentUser  = currentUser;
    }

    public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var userId = await _writeService.InviteUserAsync(
            request.TenantId,
            request.Email,
            request.DisplayName,
            request.Role,
            request.InvitedBy,
            cancellationToken);

        // Fire-and-forget audit — never throws
        await _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.InvitedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.UserInvited,
            ResourceType: "User",
            ResourceId:   userId,
            NewValueJson: $"{{\"email\":\"{request.Email}\",\"role\":\"{request.Role}\"}}"));

        return Result.Success(userId);
    }
}

