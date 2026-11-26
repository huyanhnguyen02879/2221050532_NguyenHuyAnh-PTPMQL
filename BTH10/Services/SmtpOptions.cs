namespace BTH7.Services;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "BTH10";
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
