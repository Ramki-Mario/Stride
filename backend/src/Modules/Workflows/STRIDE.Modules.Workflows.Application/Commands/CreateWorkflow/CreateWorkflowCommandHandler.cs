using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;

internal sealed class CreateWorkflowCommandHandler
    : IRequestHandler<CreateWorkflowCommand, Result<CreateWorkflowResult>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ILogger<CreateWorkflowCommandHandler> _logger;

    public CreateWorkflowCommandHandler(
        IWorkflowDefinitionRepository definitions,
        ILogger<CreateWorkflowCommandHandler> logger)
    {
        _definitions = definitions;
        _logger      = logger;
    }

    public async Task<Result<CreateWorkflowResult>> Handle(
        CreateWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // ── 1. Duplicate name guard ────────────────────────────────────────
            if (await _definitions.ExistsByNameAsync(request.Name.Trim(), cancellationToken))
            {
                _logger.CreateWorkflowDuplicateName(request.Name, request.TenantId);
                return Result.Failure<CreateWorkflowResult>(
                    $"A workflow named '{request.Name}' already exists.");
            }

            // ── 2. Create aggregate ────────────────────────────────────────────
            var definition = WorkflowDefinition.Create(
                request.TenantId,
                request.Name,
                request.Description,
                request.CreatedBy);

            // ── 3. Add steps (order = insertion order) ─────────────────────────
            foreach (var stepReq in request.Steps)
            {
                var fields = stepReq.FieldDefinitions?
                    .Select(f => (f.Label, f.FieldType, f.IsRequired, f.HelpText, f.DropdownOptions))
                    .ToList()
                    .AsReadOnly();

                definition.AddStep(
                    stepReq.Name,
                    stepReq.Description,
                    stepReq.IsRequired,
                    stepReq.RequiredRoleId,
                    fields);
            }

            await _definitions.AddAsync(definition, cancellationToken);
            await _definitions.SaveChangesAsync(cancellationToken);

            _logger.WorkflowDefinitionCreated(definition.Id, definition.Name, request.CreatedBy, request.TenantId);

            return Result.Success(new CreateWorkflowResult(
                definition.Id,
                definition.Name,
                definition.Steps.Count));
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during CreateWorkflow");
            return Result.Failure<CreateWorkflowResult>(ex.Message);
        }
    }
}
