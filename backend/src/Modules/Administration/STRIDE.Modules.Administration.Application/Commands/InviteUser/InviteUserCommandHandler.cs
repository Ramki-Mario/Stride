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

    public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken ct)
    {
        var userId = await _writeService.InviteUserAsync(
            request.TenantId,
            request.Email,
            request.DisplayName,
            request.Role,
            request.InvitedBy,
            ct);

        // Fire-and-forget audit â€” never throws
        await _audit.LogAsync(
            tenantId:     request.TenantId,
            actorId:      request.InvitedBy,
            actorEmail:   _currentUser.Email,
            action:       AuditActions.UserInvited,
            resourceType: "User",
            resourceId:   userId,
            newValueJson: $"{{\"email\":\"{request.Email}\",\"role\":\"{request.Role}\"}}");

        return Result.Success(userId);
    }
}

