using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.ReactivateUser;

internal sealed class ReactivateUserCommandHandler
    : IRequestHandler<ReactivateUserCommand, Result>
{
    private readonly IAdminWriteService _writeService;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public ReactivateUserCommandHandler(
        IAdminWriteService writeService,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _writeService = writeService;
        _audit        = audit;
        _currentUser  = currentUser;
    }

    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        await _writeService.ReactivateUserAsync(request.TenantId, request.UserId, cancellationToken);

        await _audit.LogAsync(
            tenantId:     request.TenantId,
            actorId:      _currentUser.UserId,
            actorEmail:   _currentUser.Email,
            action:       AuditActions.UserReactivated,
            resourceType: "User",
            resourceId:   request.UserId);

        return Result.Success();
    }
}

