using System.Net;
using System.Net.Mail;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RechnungsfreigabeAPI.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    Task SendEmailAsync(string to, string subject, string body, string[]? ccAddresses = null, string[]? bccAddresses = null, bool isHtml = false);
    bool IsConfigured();
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

    public bool IsConfigured()
    {
        var settings = _configuration.GetSection("Email");
        var host = settings["Host"];
        var from = settings["From"];
        return !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(from);
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        await SendEmailAsync(to, subject, body, null, null, isHtml);
    }

    public async Task SendEmailAsync(string to, string subject, string body, string[]? ccAddresses = null, string[]? bccAddresses = null, bool isHtml = false)
    {
        var settings = _configuration.GetSection("Email");
        var host = settings["Host"];
        var from = settings["From"];
        var port = settings.GetValue<int?>("Port") ?? 25;
        var username = settings["Username"];
        var password = settings["Password"];
        var enableSsl = settings.GetValue<bool?>("EnableSsl") ?? false;
        var timeout = settings.GetValue<int?>("Timeout") ?? 100000;

        if (!IsConfigured())
        {
            _logger.LogWarning("Email service not properly configured. Skipping email send to {To}", to);
            return;
        }

        try
        {
            _logger.LogInformation("Attempting to send email to {To} - Subject: {Subject}, SMTP: {Host}:{Port}, SSL: {EnableSsl}, IsHtml: {IsHtml}, BodyLength: {BodyLength}", 
                to, subject, host, port, enableSsl, isHtml, body?.Length ?? 0);
            
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Timeout = timeout,
                Credentials = string.IsNullOrWhiteSpace(username) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(username, password)
            };

            _logger.LogDebug("SMTP client configured - From: {From}, Username: {Username}, Timeout: {Timeout}ms", from, string.IsNullOrWhiteSpace(username) ? "(default)" : username, timeout);

            var mail = new MailMessage(from!, to, subject, body)
            {
                IsBodyHtml = isHtml
            };

            // Add CC addresses if provided
            if (ccAddresses != null && ccAddresses.Length > 0)
            {
                foreach (var cc in ccAddresses.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    mail.CC.Add(cc);
                }
                _logger.LogDebug("Added {Count} CC recipients: {Recipients}", ccAddresses.Length, string.Join(", ", ccAddresses));
            }

            // Add BCC addresses if provided
            if (bccAddresses != null && bccAddresses.Length > 0)
            {
                foreach (var bcc in bccAddresses.Where(b => !string.IsNullOrWhiteSpace(b)))
                {
                    mail.Bcc.Add(bcc);
                }
                _logger.LogDebug("Added {Count} BCC recipients", bccAddresses.Length);
            }

            _logger.LogInformation("Sending email via SMTP server {Host}:{Port}...", host, port);
            await client.SendMailAsync(mail);
            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            throw;
        }
    }
}
