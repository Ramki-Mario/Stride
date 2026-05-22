using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.ReactivateUser;

internal sealed class ReactivateUserCommandHandler
    : IRequestHandler<ReactivateUserCommand, Result>
{
    private readonly IAdminWriteService _writeService;

    public ReactivateUserCommandHandler(IAdminWriteService writeService)
        => _writeService = writeService;

    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken ct)
    {
        await _writeService.ReactivateUserAsync(request.TenantId, request.UserId, ct);
        return Result.Success();
    }
}
