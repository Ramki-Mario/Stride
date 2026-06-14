using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace STRIDE.Modules.Scheduling.API.Models;

public sealed class CreateScheduleRequest
{
    [Required]
    public string  Name                 { get; set; } = string.Empty;

    public string? Description          { get; set; }

    [Required, JsonRequired]
    public Guid    WorkflowDefinitionId  { get; set; }

    [Required]
    public string  CronExpression        { get; set; } = string.Empty;

    public bool    IsActive              { get; set; } = true;
}
