using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Commands.CreateKitItem;

internal sealed class CreateKitItemCommandHandler
    : IRequestHandler<CreateKitItemCommand, Result<Guid>>
{
    private readonly IKitItemRepository _kitItems;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public CreateKitItemCommandHandler(
        IKitItemRepository kitItems,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _kitItems    = kitItems;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CreateKitItemCommand request, CancellationToken cancellationToken)
    {
        if (await _kitItems.ExistsByNameAsync(request.Name, excludeId: null, cancellationToken))
            return Result.Failure<Guid>($"A kit item named '{request.Name}' already exists.");

        KitItem item;
        try
        {
            item = KitItem.Create(new NewKitItem(
                request.TenantId,
                request.Name,
                request.Category,
                request.Description,
                request.TotalQuantity,
                request.CreatedBy));
        }
        catch (KitOpsDomainException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        await _kitItems.AddAsync(item, cancellationToken);
        await _kitItems.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.CreatedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.KitItemCreated,
            ResourceType: "KitItem",
            ResourceId:   item.Id,
            NewValueJson: $"{{\"name\":\"{item.Name}\",\"category\":\"{item.Category}\"}}"));

        return Result.Success(item.Id);
    }
}
