using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.StartWorkflow;

internal sealed class StartWorkflowCommandHandler
    : IRequestHandler<StartWorkflowCommand, Result<StartWorkflowResult>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowInstanceRepository   _instances;
    private readonly ILogger<StartWorkflowCommandHandler> _logger;

    public StartWorkflowCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowInstanceRepository   instances,
        ILogger<StartWorkflowCommandHandler> logger)
    {
        _definitions = definitions;
        _instances   = instances;
        _logger      = logger;
    }

    public async Task<Result<StartWorkflowResult>> Handle(
        StartWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var definition = await _definitions.GetByIdAsync(request.WorkflowDefinitionId, cancellationToken);
            if (definition is null)
            {
                return Result.Failure<StartWorkflowResult>(
                    $"Workflow definition '{request.WorkflowDefinitionId}' not found.");
            }

            var instance = WorkflowInstance.Start(definition, request.StartedBy, request.ClientId);

            await _instances.AddAsync(instance, cancellationToken);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.WorkflowInstanceStarted(instance.Id, definition.Id, request.StartedBy);

            return Result.Success(new StartWorkflowResult(
                instance.Id,
                definition.Id,
                instance.WorkflowName,
                instance.Steps.Count));
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during StartWorkflow");
            return Result.Failure<StartWorkflowResult>(ex.Message);
        }
    }
}
