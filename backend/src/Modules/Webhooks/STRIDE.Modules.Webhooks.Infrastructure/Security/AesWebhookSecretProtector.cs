using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using STRIDE.Modules.Webhooks.Application.Abstractions;

namespace STRIDE.Modules.Webhooks.Infrastructure.Security;

/// <summary>
/// AES-256-CBC encryption of webhook signing secrets at rest.
///
/// The key comes from <c>Webhooks:EncryptionKey</c> (base64-encoded 32 bytes) and
/// MUST live only in the gitignored appsettings.Development.json locally, or a
/// secret store / environment variable in deployed environments — never in a
/// committed config file. A fresh random IV is generated per encryption and
/// prepended to the ciphertext, so the stored form is base64(IV ‖ ciphertext).
/// Fails fast at startup if the key is missing or the wrong length, mirroring the
/// JWT secret handling.
/// </summary>
public sealed class AesWebhookSecretProtector : IWebhookSecretProtector
{
    private readonly byte[] _key;

    public AesWebhookSecretProtector(IConfiguration configuration)
    {
        var raw = configuration["Webhooks:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "Webhooks:EncryptionKey is not configured. Set a base64-encoded 32-byte key in " +
                "appsettings.Development.json (gitignored) or the Webhooks__EncryptionKey environment variable.");

        try
        {
            _key = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Webhooks:EncryptionKey must be a valid base64 string.");
        }

        if (_key.Length != 32)
            throw new InvalidOperationException(
                $"Webhooks:EncryptionKey must decode to 32 bytes (AES-256); got {_key.Length}.");
    }

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        var plainBytes  = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = aes.EncryptCbc(plainBytes, aes.IV);

        var combined = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public string Unprotect(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

        var combined = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;

        var ivLength = aes.BlockSize / 8; // 16 bytes for AES
        var iv         = new byte[ivLength];
        var cipherBytes = new byte[combined.Length - ivLength];
        Buffer.BlockCopy(combined, 0, iv, 0, ivLength);
        Buffer.BlockCopy(combined, ivLength, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = aes.DecryptCbc(cipherBytes, iv);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
