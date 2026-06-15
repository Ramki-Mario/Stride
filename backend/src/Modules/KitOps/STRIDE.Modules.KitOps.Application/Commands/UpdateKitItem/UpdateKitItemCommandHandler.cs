using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Commands.UpdateKitItem;

internal sealed class UpdateKitItemCommandHandler
    : IRequestHandler<UpdateKitItemCommand, Result>
{
    private readonly IKitItemRepository _kitItems;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public UpdateKitItemCommandHandler(
        IKitItemRepository kitItems,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _kitItems    = kitItems;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateKitItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _kitItems.GetByIdAsync(request.KitItemId, cancellationToken);
        if (item is null)
            return Result.Failure($"Kit item '{request.KitItemId}' not found.");

        if (await _kitItems.ExistsByNameAsync(request.Name, excludeId: request.KitItemId, cancellationToken))
            return Result.Failure($"A kit item named '{request.Name}' already exists.");

        try
        {
            item.Update(request.Name, request.Category, request.Description, request.TotalQuantity, request.UpdatedBy);
        }
        catch (KitOpsDomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _kitItems.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.UpdatedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.KitItemUpdated,
            ResourceType: "KitItem",
            ResourceId:   item.Id,
            NewValueJson: $"{{\"name\":\"{item.Name}\",\"category\":\"{item.Category}\"}}"));

        return Result.Success();
    }
}
