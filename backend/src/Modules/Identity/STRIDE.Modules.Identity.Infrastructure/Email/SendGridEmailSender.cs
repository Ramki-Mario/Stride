using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.Email;

internal sealed class SendGridEmailSender : IEmailSender
{
    private readonly SendGridOptions               _options;
    private readonly ILogger<SendGridEmailSender>  _logger;

    public SendGridEmailSender(
        IOptions<SendGridOptions>             options,
        ILogger<SendGridEmailSender>          logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var client  = new SendGridClient(_options.ApiKey);
        var from    = new EmailAddress(_options.FromEmail, _options.FromName);
        var toAddr  = new EmailAddress(to);
        var message = MailHelper.CreateSingleEmail(from, toAddr, subject, plainTextContent: null, htmlContent: htmlBody);

        var response = await client.SendEmailAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SendGrid returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            }
        }
        else if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Email sent to {To} via SendGrid.", to);
        }
    }
}
