using System.Net;
using System.Net.Mail;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace RechnungsfreigabeAPI.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    Task SendEmailAsync(string to, string subject, string body, string[]? ccAddresses = null, string[]? bccAddresses = null, bool isHtml = false);
    bool IsConfigured();
}
