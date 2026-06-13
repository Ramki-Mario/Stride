namespace STRIDE.Modules.Reporting.Application.ReadModels;

/// <summary>A workflow definition summary used to populate analytics filter dropdowns.</summary>
public sealed record AnalyticsWorkflowDefinitionDto(Guid Id, string Name);

/// <summary>Average completion time for a single workflow definition.</summary>
public sealed record WorkflowCompletionTimeDto(
    Guid   DefinitionId,
    string DefinitionName,
    double AvgDurationMinutes,
    int    InstanceCount);

/// <summary>Average time to complete a single step across all instances (bottleneck signal).</summary>
public sealed record StepBottleneckDto(
    string StepName,
    double AvgDurationMinutes,
    int    OccurrenceCount);

/// <summary>Average completion time for a single calendar week.</summary>
public sealed record CompletionTrendDto(
    DateOnly WeekStart,
    double   AvgDurationMinutes,
    int      InstanceCount);

/// <summary>Top-level response for the completion-time analytics endpoint.</summary>
public sealed record CompletionTimeAnalyticsDto(
    IReadOnlyList<WorkflowCompletionTimeDto> ByDefinition,
    IReadOnlyList<StepBottleneckDto>         Bottlenecks,
    IReadOnlyList<CompletionTrendDto>        WeeklyTrend);

/// <summary>Flat row used for CSV export of individual completed instances.</summary>
public sealed record CompletionTimeExportRowDto(
    Guid     InstanceId,
    string   WorkflowName,
    DateTime StartedAt,
    DateTime CompletedAt,
    double   DurationMinutes);
