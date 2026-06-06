using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateUserRole;

internal sealed class UpdateUserRoleCommandHandler
    : IRequestHandler<UpdateUserRoleCommand, Result>
{
    private readonly IAdminWriteService _writeService;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public UpdateUserRoleCommandHandler(
        IAdminWriteService writeService,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _writeService = writeService;
        _audit        = audit;
        _currentUser  = currentUser;
    }

    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        await _writeService.UpdateUserRoleAsync(
            request.TenantId, request.UserId, request.NewRole, request.UpdatedBy, cancellationToken);

        await _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.UpdatedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.UserRoleChanged,
            ResourceType: "User",
            ResourceId:   request.UserId,
            NewValueJson: $"{{\"role\":\"{request.NewRole}\"}}"));

        return Result.Success();
    }
}

