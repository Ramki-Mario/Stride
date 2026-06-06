using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.DeactivateUser;

internal sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IAdminWriteService _writeService;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public DeactivateUserCommandHandler(
        IAdminWriteService writeService,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _writeService = writeService;
        _audit        = audit;
        _currentUser  = currentUser;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == request.CallerUserId)
            return Result.Failure("You cannot deactivate your own account.");

        await _writeService.DeactivateUserAsync(request.TenantId, request.UserId, cancellationToken);

        await _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.CallerUserId,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.UserDeactivated,
            ResourceType: "User",
            ResourceId:   request.UserId));

        return Result.Success();
    }
}

