using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.Email;

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
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[DEV EMAIL] To: {To} | Subject: {Subject}\n{Body}",
                to, subject, htmlBody);
        }

        return Task.CompletedTask;
    }
}
