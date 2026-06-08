using System.Text.Json;
using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

/// <summary>
/// Describes a single data field attached to a <see cref="StepDefinition"/>.
/// At step-completion time each field definition drives a form control that captures
/// structured work data (hours logged, materials used, date observed, etc.).
/// </summary>
public sealed class StepFieldDefinition : BaseEntity<Guid>
{
    /// <summary>FK to the owning <see cref="StepDefinition"/>.</summary>
    public Guid StepDefinitionId { get; private set; }

    /// <summary>Human-readable label shown above the input at runtime.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Determines the input control and validation rules.</summary>
    public StepFieldType FieldType { get; private set; }

    /// <summary>Whether the user must supply a value before the step can be completed.</summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Zero-based sort order within the step's field list.
    /// Managed by the parent <see cref="StepDefinition"/>.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Optional secondary text shown below the input to guide the user.</summary>
    public string? HelpText { get; private set; }

    /// <summary>
    /// Raw JSON array of option strings — only populated when
    /// <see cref="FieldType"/> is <see cref="StepFieldType.Dropdown"/>.
    /// EF Core maps this column; callers use <see cref="DropdownOptions"/> instead.
    /// </summary>
    public string? DropdownOptionsJson { get; private set; }

    // ── Computed ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Deserialized dropdown options. Empty list for non-Dropdown fields.
    /// </summary>
    public IReadOnlyList<string> DropdownOptions =>
        DropdownOptionsJson is null
            ? (IReadOnlyList<string>)Array.Empty<string>()
            : JsonSerializer.Deserialize<List<string>>(DropdownOptionsJson)
              ?? (IReadOnlyList<string>)Array.Empty<string>();

    // ── Construction ─────────────────────────────────────────────────────────

    private StepFieldDefinition() { }

    internal static StepFieldDefinition Create(
        Guid stepDefinitionId,
        string label,
        StepFieldType fieldType,
        bool isRequired,
        int displayOrder,
        string? helpText,
        IReadOnlyList<string>? dropdownOptions)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new WorkflowDomainException("Field label cannot be empty.");

        if (fieldType == StepFieldType.Dropdown
            && (dropdownOptions == null || dropdownOptions.Count < 2))
        {
            throw new WorkflowDomainException(
                "Dropdown fields require at least 2 options.");
        }

        return new StepFieldDefinition
        {
            Id                  = Guid.NewGuid(),
            StepDefinitionId    = stepDefinitionId,
            Label               = label.Trim(),
            FieldType           = fieldType,
            IsRequired          = isRequired,
            DisplayOrder        = displayOrder,
            HelpText            = helpText?.Trim(),
            DropdownOptionsJson = dropdownOptions is not null
                ? JsonSerializer.Serialize(dropdownOptions)
                : null,
        };
    }

    // ── Mutation helpers (called by StepDefinition) ──────────────────────────

    internal void SetDisplayOrder(int order) => DisplayOrder = order;

    internal void Update(
        string label,
        StepFieldType fieldType,
        bool isRequired,
        string? helpText,
        IReadOnlyList<string>? dropdownOptions)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new WorkflowDomainException("Field label cannot be empty.");

        if (fieldType == StepFieldType.Dropdown
            && (dropdownOptions == null || dropdownOptions.Count < 2))
        {
            throw new WorkflowDomainException(
                "Dropdown fields require at least 2 options.");
        }

        Label               = label.Trim();
        FieldType           = fieldType;
        IsRequired          = isRequired;
        HelpText            = helpText?.Trim();
        DropdownOptionsJson = dropdownOptions is not null
            ? JsonSerializer.Serialize(dropdownOptions)
            : null;
    }
}
