using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.DeleteWorkflow;

internal sealed class DeleteWorkflowCommandHandler
    : IRequestHandler<DeleteWorkflowCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ILogger<DeleteWorkflowCommandHandler> _logger;

    public DeleteWorkflowCommandHandler(
        IWorkflowDefinitionRepository definitions,
        ILogger<DeleteWorkflowCommandHandler> logger)
    {
        _definitions = definitions;
        _logger      = logger;
    }

    public async Task<Result> Handle(DeleteWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var definition = await _definitions.GetByIdAsync(request.WorkflowDefinitionId, cancellationToken);
            if (definition is null)
            {
                return Result.Failure(
                    $"Workflow definition '{request.WorkflowDefinitionId}' not found.");
            }

            definition.Delete(request.DeletedBy);
            _definitions.Update(definition);
            await _definitions.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "WorkflowDefinition {Id} soft-deleted by {UserId}",
                definition.Id, request.DeletedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during DeleteWorkflow");
            return Result.Failure(ex.Message);
        }
    }
}
