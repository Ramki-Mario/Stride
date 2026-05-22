using STRIDE.BuildingBlocks.Domain.Primitives;

namespace STRIDE.Modules.Invoicing.Domain.ValueObjects;

/// <summary>
/// Immutable monetary amount with a currency code.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount   { get; }
    public string  Currency { get; }   // ISO 4217, e.g. "USD"

    private Money() { Currency = string.Empty; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code is required.", nameof(currency));

        Amount   = Math.Round(amount, 2);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot mix currencies: {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
