using STRIDE.BuildingBlocks.Domain.Primitives;

namespace STRIDE.Modules.Identity.Domain.ValueObjects;

public sealed class Password : ValueObject
{
    public string Hash { get; }

    private Password(string hash) => Hash = hash;

    public static Password FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(hash));

        return new Password(hash);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Hash;
    }

    public override string ToString() => Hash;
}
