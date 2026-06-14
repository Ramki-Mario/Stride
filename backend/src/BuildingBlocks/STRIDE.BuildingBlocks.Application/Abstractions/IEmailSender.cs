namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstraction for transactional email delivery. Implementations are swappable
/// (SendGrid in production, console stub in development) without changing callers.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
