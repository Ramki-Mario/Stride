namespace STRIDE.Modules.Invoicing.Domain.Enums;

/// <summary>
/// Lifecycle status of an Invoice.
/// Valid transitions:
///   Draft → Sent → Paid
///   Draft → Void
///   Sent  → Void
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Invoice is being prepared — not yet sent to the client.</summary>
    Draft = 0,

    /// <summary>Invoice has been issued to the client and payment is pending.</summary>
    Sent = 1,

    /// <summary>Invoice has been fully paid.</summary>
    Paid = 2,

    /// <summary>Invoice was cancelled and is no longer valid.</summary>
    Void = 3,
}
