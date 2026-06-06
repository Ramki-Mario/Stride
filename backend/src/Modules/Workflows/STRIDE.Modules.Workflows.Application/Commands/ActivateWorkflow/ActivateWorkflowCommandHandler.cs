using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.ActivateWorkflow;

internal sealed class ActivateWorkflowCommandHandler
    : IRequestHandler<ActivateWorkflowCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ILogger<ActivateWorkflowCommandHandler> _logger;

    public ActivateWorkflowCommandHandler(
        IWorkflowDefinitionRepository definitions,
        ILogger<ActivateWorkflowCommandHandler> logger)
    {
        _definitions = definitions;
        _logger      = logger;
    }

    public async Task<Result> Handle(ActivateWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var definition = await _definitions.GetByIdAsync(request.WorkflowDefinitionId, cancellationToken);
            if (definition is null)
                return Result.Failure($"Workflow definition '{request.WorkflowDefinitionId}' not found.");

            definition.Activate(request.ActivatedBy);
            _definitions.Update(definition);
            await _definitions.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "WorkflowDefinition {Id} activated by {UserId}", definition.Id, request.ActivatedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during ActivateWorkflow");
            return Result.Failure(ex.Message);
        }
    }
}
