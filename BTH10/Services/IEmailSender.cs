namespace BTH7.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string recipient, string subject, string htmlMessage);
}
