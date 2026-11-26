using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace BTH7.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string recipient, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            throw new InvalidOperationException(
                "Email delivery is not configured. Set Smtp:Host and Smtp:SenderEmail using environment variables or User Secrets.");
        }

        if (_options.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException("Smtp:Port must be between 1 and 65535.");
        }

        if (!string.IsNullOrWhiteSpace(_options.UserName) &&
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "Smtp:Password is required when Smtp:UserName is configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        try
        {
            await client.SendMailAsync(message);
        }
        catch (SmtpException exception)
        {
            _logger.LogError(exception, "Could not send account email to {Recipient}.", recipient);
            throw;
        }

        _logger.LogInformation("Sent account email to {Recipient} with subject {Subject}.", recipient, subject);
    }
}
