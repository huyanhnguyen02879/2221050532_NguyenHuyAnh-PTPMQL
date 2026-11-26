namespace BTH7.Services;

public class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string recipient, string subject, string htmlMessage)
    {
        _logger.LogInformation(
            "Development email to {Recipient}. Subject: {Subject}. Message: {Message}",
            recipient,
            subject,
            htmlMessage);

        return Task.CompletedTask;
    }
}
