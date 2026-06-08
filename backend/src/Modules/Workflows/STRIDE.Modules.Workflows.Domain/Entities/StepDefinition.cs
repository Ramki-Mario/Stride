using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class StepDefinition : BaseEntity<Guid>
{
    private readonly List<StepFieldDefinition> _fields = new();

    public Guid  WorkflowDefinitionId { get; private set; }
    public string  Name               { get; private set; } = string.Empty;
    public string? Description        { get; private set; }
    public int     Order              { get; private set; }
    public bool    IsRequired         { get; private set; }
    /// <summary>
    /// Optional cross-module reference to an Identity Role.
    /// When set, only users who hold this role may be auto-assigned or claim this step.
    /// Stored as a bare FK (no EF navigation — Identity is a separate DbContext).
    /// </summary>
    public Guid? RequiredRoleId { get; private set; }

    /// <summary>
    /// Ordered list of data-capture fields defined on this step.
    /// Populated at workflow-definition time; rendered as a form at step-completion time.
    /// </summary>
    public IReadOnlyList<StepFieldDefinition> Fields => _fields.AsReadOnly();

    private StepDefinition() { }

    internal static StepDefinition Create(
        Guid workflowDefinitionId,
        string name,
        string? description,
        int order,
        bool isRequired = true,
        Guid? requiredRoleId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowDomainException("Step name cannot be empty.");

        if (order < 0)
            throw new WorkflowDomainException("Step order must be a non-negative integer.");

        return new StepDefinition
        {
            Id                   = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            Name                 = name.Trim(),
            Description          = description?.Trim(),
            Order                = order,
            IsRequired           = isRequired,
            RequiredRoleId       = requiredRoleId,
        };
    }

    internal void Update(string name, string? description, bool isRequired, Guid? requiredRoleId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowDomainException("Step name cannot be empty.");

        Name           = name.Trim();
        Description    = description?.Trim();
        IsRequired     = isRequired;
        RequiredRoleId = requiredRoleId;
    }

    // ── Field definition management ──────────────────────────────────────────

    /// <summary>
    /// Adds a new data-capture field to this step.
    /// Fields are appended in insertion order; <see cref="StepFieldDefinition.DisplayOrder"/>
    /// equals the zero-based index at the time of insertion.
    /// </summary>
    internal StepFieldDefinition AddField(
        string label,
        StepFieldType fieldType,
        bool isRequired,
        string? helpText = null,
        IReadOnlyList<string>? dropdownOptions = null)
    {
        var order = _fields.Count;
        var field = StepFieldDefinition.Create(Id, label, fieldType, isRequired, order, helpText, dropdownOptions);
        _fields.Add(field);
        return field;
    }

    /// <summary>Removes the field and re-numbers remaining fields consecutively.</summary>
    internal void RemoveField(Guid fieldId)
    {
        var field = _fields.FirstOrDefault(f => f.Id == fieldId)
            ?? throw new WorkflowDomainException($"Field '{fieldId}' not found on step.");

        _fields.Remove(field);
        RenumberFields();
    }

    /// <summary>Moves the specified field one position earlier in the display order.</summary>
    internal void MoveFieldUp(Guid fieldId)
    {
        var index = _fields.FindIndex(f => f.Id == fieldId);
        if (index <= 0) return;
        Swap(index - 1, index);
    }

    /// <summary>Moves the specified field one position later in the display order.</summary>
    internal void MoveFieldDown(Guid fieldId)
    {
        var index = _fields.FindIndex(f => f.Id == fieldId);
        if (index < 0 || index >= _fields.Count - 1) return;
        Swap(index, index + 1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Swap(int a, int b)
    {
        (_fields[a], _fields[b]) = (_fields[b], _fields[a]);
        _fields[a].SetDisplayOrder(a);
        _fields[b].SetDisplayOrder(b);
    }

    private void RenumberFields()
    {
        for (var i = 0; i < _fields.Count; i++)
            _fields[i].SetDisplayOrder(i);
    }
}
