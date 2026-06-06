using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Domain.Tests.Builders;

/// <summary>
/// Fluent builder for <see cref="WorkflowDefinition"/> test fixtures.
/// Produces a valid Draft definition by default — chain methods to customise.
/// </summary>
internal sealed class WorkflowDefinitionBuilder
{
    private Guid    _tenantId   = Guid.NewGuid();
    private string  _name       = "Test Workflow";
    private string? _description = "A test workflow description";
    private Guid    _createdBy  = Guid.NewGuid();
    private int     _stepCount  = 0;

    public WorkflowDefinitionBuilder WithTenantId(Guid tenantId)    { _tenantId   = tenantId;   return this; }
    public WorkflowDefinitionBuilder WithName(string name)           { _name       = name;       return this; }
    public WorkflowDefinitionBuilder WithDescription(string? desc)   { _description = desc;      return this; }
    public WorkflowDefinitionBuilder WithCreatedBy(Guid userId)      { _createdBy  = userId;     return this; }
    public WorkflowDefinitionBuilder WithSteps(int count)            { _stepCount  = count;      return this; }

    public WorkflowDefinition Build()
    {
        var def = WorkflowDefinition.Create(_tenantId, _name, _description, _createdBy);
        def.ClearDomainEvents();

        for (var i = 0; i < _stepCount; i++)
            def.AddStep($"Step {i + 1}", $"Description for step {i + 1}");

        def.ClearDomainEvents();
        return def;
    }

    /// <summary>Returns a valid Draft definition with one step, pre-cleared of events.</summary>
    public static WorkflowDefinition DraftWithOneStep()
        => new WorkflowDefinitionBuilder().WithSteps(1).Build();

    /// <summary>Returns a definition that has been activated (Draft → Active).</summary>
    public static WorkflowDefinition Active()
    {
        var def = new WorkflowDefinitionBuilder().WithSteps(1).Build();
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();
        return def;
    }
}
