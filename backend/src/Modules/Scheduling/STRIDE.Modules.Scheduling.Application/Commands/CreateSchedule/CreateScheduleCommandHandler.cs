using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Application.DTOs;
using STRIDE.Modules.Scheduling.Domain.Entities;
using STRIDE.Modules.Scheduling.Domain.ValueObjects;
using CronosExpression = Cronos.CronExpression;

namespace STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;

internal sealed class CreateScheduleCommandHandler
    : IRequestHandler<CreateScheduleCommand, Result<ScheduleDefinitionDto>>
{
    private readonly IScheduleDefinitionRepository _repository;
    private readonly ICurrentUser                  _currentUser;
    private readonly ITenantContext                _tenant;

    public CreateScheduleCommandHandler(
        IScheduleDefinitionRepository repository,
        ICurrentUser                  currentUser,
        ITenantContext                 tenant)
    {
        _repository  = repository;
        _currentUser = currentUser;
        _tenant      = tenant;
    }

    public async Task<Result<ScheduleDefinitionDto>> Handle(
        CreateScheduleCommand request,
        CancellationToken     cancellationToken)
    {
        var cronVo = CronExpression.Create(request.CronExpression);

        DateTime? nextRunAt = null;
        if (request.IsActive)
            nextRunAt = ComputeNextRun(request.CronExpression);

        var schedule = ScheduleDefinition.Create(
            tenantId:             _tenant.TenantId,
            name:                 request.Name,
            description:          request.Description,
            workflowDefinitionId: request.WorkflowDefinitionId,
            cronExpression:       cronVo,
            isActive:             request.IsActive,
            nextRunAt:            nextRunAt,
            createdBy:            _currentUser.UserId);

        await _repository.AddAsync(schedule, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(schedule));
    }

    internal static DateTime? ComputeNextRun(string expression)
    {
        try
        {
            var cron = CronosExpression.Parse(expression, Cronos.CronFormat.Standard);
            return cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }
        catch
        {
            return null;
        }
    }

    internal static ScheduleDefinitionDto ToDto(ScheduleDefinition s) => new(
        Id:                   s.Id,
        Name:                 s.Name,
        Description:          s.Description,
        WorkflowDefinitionId: s.WorkflowDefinitionId,
        CronExpression:       s.CronExpression.Value,
        IsActive:             s.IsActive,
        NextRunAt:            s.NextRunAt,
        CreatedAt:            s.CreatedAt,
        UpdatedAt:            s.UpdatedAt);
}
