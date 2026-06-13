namespace STRIDE.Modules.Webhooks.Application.Abstractions;

/// <summary>
/// Encrypts/decrypts webhook signing secrets at the persistence boundary.
/// Implemented in Infrastructure (AES-256) and wired into the EF value converter
/// so secrets are never stored as plaintext at rest.
/// </summary>
public interface IWebhookSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
