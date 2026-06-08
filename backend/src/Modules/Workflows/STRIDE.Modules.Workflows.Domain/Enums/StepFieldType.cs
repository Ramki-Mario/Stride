namespace STRIDE.Modules.Workflows.Domain.Enums;

/// <summary>
/// The data type of a field that can be attached to a step definition.
/// Determines the input control rendered at runtime and the validation rules applied.
/// </summary>
public enum StepFieldType
{
    /// <summary>Single-line free text.</summary>
    Text,

    /// <summary>Numeric value (integer or decimal).</summary>
    Number,

    /// <summary>Monetary value in the tenant's base currency.</summary>
    Currency,

    /// <summary>Duration in hours (decimal, e.g. 1.5 = 90 minutes).</summary>
    Hours,

    /// <summary>Single-select from a predefined list of options.</summary>
    Dropdown,

    /// <summary>Calendar date (no time component).</summary>
    Date,

    /// <summary>Yes / No toggle.</summary>
    Boolean,

    /// <summary>Multi-line notes / comments field.</summary>
    LongText,
}
