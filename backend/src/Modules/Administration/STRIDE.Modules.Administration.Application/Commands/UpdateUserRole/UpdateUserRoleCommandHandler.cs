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

    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        await _writeService.UpdateUserRoleAsync(
            request.TenantId, request.UserId, request.NewRole, request.UpdatedBy, ct);

        await _audit.LogAsync(
            tenantId:     request.TenantId,
            actorId:      request.UpdatedBy,
            actorEmail:   _currentUser.Email,
            action:       AuditActions.UserRoleChanged,
            resourceType: "User",
            resourceId:   request.UserId,
            newValueJson: $"{{\"role\":\"{request.NewRole}\"}}");

        return Result.Success();
    }
}

