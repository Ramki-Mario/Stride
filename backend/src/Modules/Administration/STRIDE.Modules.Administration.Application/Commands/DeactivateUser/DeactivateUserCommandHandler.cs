using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.DeactivateUser;

internal sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IAdminWriteService _writeService;

    public DeactivateUserCommandHandler(IAdminWriteService writeService)
        => _writeService = writeService;

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        if (request.UserId == request.CallerUserId)
            return Result.Failure("You cannot deactivate your own account.");

        await _writeService.DeactivateUserAsync(request.TenantId, request.UserId, ct);
        return Result.Success();
    }
}
