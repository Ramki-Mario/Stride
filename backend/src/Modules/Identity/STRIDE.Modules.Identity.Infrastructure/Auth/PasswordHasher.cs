using System.Security.Cryptography;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// PBKDF2-SHA256 password hasher. No external packages — uses only
/// <see cref="System.Security.Cryptography"/> built-in to .NET.
///
/// Stored format: <c>{iterations}.{base64(salt)}.{base64(hash)}</c>
/// Iterations and salt are embedded so the stored value is self-describing
/// and allows algorithm parameters to be upgraded in future without
/// breaking existing hashes.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize       = 16;       // 128-bit salt
    private const int KeySize        = 32;       // 256-bit derived key
    private const int DefaultIter    = 100_000;  // NIST-recommended minimum for PBKDF2-SHA256
    private static readonly HashAlgorithmName _algorithm = HashAlgorithmName.SHA256;

    public Password Hash(string plainTextPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key  = Rfc2898DeriveBytes.Pbkdf2(
            plainTextPassword, salt, DefaultIter, _algorithm, KeySize);

        var stored = $"{DefaultIter}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        return Password.FromHash(stored);
    }

    public bool Verify(string plainTextPassword, Password passwordHash)
    {
        var parts = passwordHash.Hash.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out var iterations) || iterations <= 0)
            return false;

        byte[] salt, storedKey;
        try
        {
            salt      = Convert.FromBase64String(parts[1]);
            storedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            plainTextPassword, salt, iterations, _algorithm, storedKey.Length);

        // Constant-time comparison prevents timing attacks.
        return CryptographicOperations.FixedTimeEquals(derivedKey, storedKey);
    }
}
