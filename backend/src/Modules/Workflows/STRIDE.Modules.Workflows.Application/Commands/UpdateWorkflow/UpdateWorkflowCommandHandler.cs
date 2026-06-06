using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.UpdateWorkflow;

internal sealed class UpdateWorkflowCommandHandler
    : IRequestHandler<UpdateWorkflowCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ILogger<UpdateWorkflowCommandHandler> _logger;

    public UpdateWorkflowCommandHandler(
        IWorkflowDefinitionRepository definitions,
        ILogger<UpdateWorkflowCommandHandler> logger)
    {
        _definitions = definitions;
        _logger      = logger;
    }

    public async Task<Result> Handle(UpdateWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var definition = await _definitions.GetByIdAsync(request.WorkflowDefinitionId, cancellationToken);
            if (definition is null)
            {
                return Result.Failure(
                    $"Workflow definition '{request.WorkflowDefinitionId}' not found.");
            }

            definition.Update(request.Name, request.Description);
            _definitions.Update(definition);
            await _definitions.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "WorkflowDefinition {Id} updated by {UserId}",
                definition.Id, request.UpdatedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during UpdateWorkflow");
            return Result.Failure(ex.Message);
        }
    }
}
