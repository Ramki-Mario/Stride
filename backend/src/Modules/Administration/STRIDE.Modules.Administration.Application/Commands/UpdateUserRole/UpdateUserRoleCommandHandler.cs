using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateUserRole;

internal sealed class UpdateUserRoleCommandHandler
    : IRequestHandler<UpdateUserRoleCommand, Result>
{
    private readonly IAdminWriteService _writeService;

    public UpdateUserRoleCommandHandler(IAdminWriteService writeService)
        => _writeService = writeService;

    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        await _writeService.UpdateUserRoleAsync(
            request.TenantId, request.UserId, request.NewRole, request.UpdatedBy, ct);

        return Result.Success();
    }
}
