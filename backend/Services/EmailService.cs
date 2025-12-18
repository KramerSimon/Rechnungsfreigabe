using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RechnungsfreigabeAPI.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var settings = _configuration.GetSection("Email");
        var host = settings["Host"];
        var from = settings["From"];
        var port = settings.GetValue<int?>("Port") ?? 25;
        var username = settings["Username"];
        var password = settings["Password"];
        var enableSsl = settings.GetValue<bool?>("EnableSsl") ?? false;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("Email not sent: missing SMTP configuration (Host/From). Subject: {Subject}", subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrWhiteSpace(username) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(username, password)
            };

            var mail = new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = false
            };

            await client.SendMailAsync(mail);
            _logger.LogInformation("Email sent to {Recipient}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}: {Subject}", to, subject);
        }
    }
}
