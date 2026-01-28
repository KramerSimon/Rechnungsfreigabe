using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;


namespace RechnungsfreigabeAPI.Services.Interfaces;

public interface IEscalationEmailService
{
    Task<bool> SendEscalationEmailAsync(int invoiceId, int escalationRuleId);
    Task ProcessEscalationEmailsAsync();
    Task EnsureEscalationTrackingAsync(int invoiceId, int escalationRuleId);
}
