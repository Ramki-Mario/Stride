using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.ExportStepFieldValuesCsv;

public sealed record ExportStepFieldValuesCsvQuery(Guid WorkflowInstanceId)
    : IRequest<Result<string>>;
