using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.Email;

/// <summary>
/// Development stub — logs email content to the console instead of sending.
/// Never used in production (switched out by DI registration in IdentityInfrastructureExtensions).
/// </summary>
internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(ILogger<SmtpEmailSender> logger) => _logger = logger;

    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] To: {To} | Subject: {Subject}\n{Body}",
            to, subject, htmlBody);

        return Task.CompletedTask;
    }
}
