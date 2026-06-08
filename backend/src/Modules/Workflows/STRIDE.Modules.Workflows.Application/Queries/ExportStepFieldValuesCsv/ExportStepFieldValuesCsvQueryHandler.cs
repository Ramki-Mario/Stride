using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Queries.ExportStepFieldValuesCsv;

internal sealed class ExportStepFieldValuesCsvQueryHandler
    : IRequestHandler<ExportStepFieldValuesCsvQuery, Result<string>>
{
    private readonly IWorkflowInstanceRepository   _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ILogger<ExportStepFieldValuesCsvQueryHandler> _logger;

    public ExportStepFieldValuesCsvQueryHandler(
        IWorkflowInstanceRepository   instances,
        IWorkflowDefinitionRepository definitions,
        ILogger<ExportStepFieldValuesCsvQueryHandler> logger)
    {
        _instances   = instances;
        _definitions = definitions;
        _logger      = logger;
    }

    public async Task<Result<string>> Handle(
        ExportStepFieldValuesCsvQuery request,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
        if (instance is null)
        {
            _logger.LogWarning(
                "ExportStepFieldValuesCsv: instance {Id} not found", request.WorkflowInstanceId);
            return Result.Failure<string>(
                $"Workflow instance '{request.WorkflowInstanceId}' not found.");
        }

        var definition = await _definitions.GetByIdAsync(instance.WorkflowDefinitionId, cancellationToken);

        // Build a flat lookup: fieldDefinitionId → (label, fieldType string)
        var fieldMeta = new Dictionary<Guid, (string Label, string FieldType)>();
        if (definition is not null)
        {
            foreach (var step in definition.Steps)
            foreach (var field in step.Fields)
                fieldMeta[field.Id] = (field.Label, field.FieldType.ToString());
        }

        var sb = new StringBuilder();
        sb.AppendLine("StepOrder,StepName,FieldLabel,FieldType,Value");

        foreach (var step in instance.Steps.OrderBy(s => s.Order))
        {
            foreach (var fv in step.FieldValues)
            {
                fieldMeta.TryGetValue(fv.StepFieldDefinitionId, out var meta);
                sb.AppendLine(string.Join(',',
                    Q(step.Order.ToString()),
                    Q(step.StepName),
                    Q(meta.Label ?? fv.StepFieldDefinitionId.ToString("N")),
                    Q(meta.FieldType ?? string.Empty),
                    Q(fv.Value)));
            }
        }

        return Result.Success(sb.ToString());
    }

    /// <summary>RFC 4180 CSV quoting — wraps value in double-quotes and escapes internal quotes.</summary>
    private static string Q(string value)
        => '"' + (value ?? string.Empty).Replace("\"", "\"\"") + '"';
}
