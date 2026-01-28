using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace RechnungsfreigabeAPI.Services;

public interface IEscalationEmailService
{
    Task<bool> SendEscalationEmailAsync(int invoiceId, int escalationRuleId);
    Task ProcessEscalationEmailsAsync();
    Task EnsureEscalationTrackingAsync(int invoiceId, int escalationRuleId);
}

/// <summary>
/// Service to handle sending emails based on escalation rules for invoices
/// </summary>
public class EscalationEmailService : IEscalationEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<EscalationEmailService> _logger;
    private readonly IConfiguration _configuration;

    public EscalationEmailService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<EscalationEmailService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send escalation email immediately for a specific invoice and rule
    /// Returns true if at least one email was sent successfully
    /// </summary>
    public async Task<bool> SendEscalationEmailAsync(int invoiceId, int escalationRuleId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.CostCenter)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                return false;

            var rule = await _context.EscalationRules
                .Include(r => r.NotifyUsers)
                .Include(r => r.NotifyRoles)
                .FirstOrDefaultAsync(r => r.Id == escalationRuleId);

            if (rule == null)
                return false;

            if (!rule.IsActive)
                return false;

            // Get recipients
            var recipients = await GetRecipientEmailsAsync(rule);
            if (!recipients.Any())
                return false;

            // Generate email content
            var emailContent = GenerateEmailContent(invoice, rule);
            var subject = $"ESCALATION: Invoice {invoice.InvoiceNumber} requires attention";

            int emailsSent = 0;
            // Send email to each recipient
            foreach (var recipient in recipients)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        recipient.Email,
                        subject,
                        emailContent,
                        isHtml: true
                    );

                    emailsSent++;
                    // Track the escalation
                    await LogEscalationAsync(invoiceId, escalationRuleId, recipient.Id);
                }
                catch (Exception)
                {
                    // Silently continue if email send fails for this recipient
                }
            }
            
            return emailsSent > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Process escalation emails for invoices that meet escalation criteria
    /// This should be called periodically (e.g., every hour) via a background job
    /// </summary>
    public async Task ProcessEscalationEmailsAsync()
    {
        try
        {
            var activeRules = await _context.EscalationRules
                .Include(r => r.TriggerStatuses)
                .Include(r => r.NotifyUsers)
                .Include(r => r.NotifyRoles)
                .Where(r => r.IsActive)
                .ToListAsync();

            if (!activeRules.Any())
                return;

            // Get all invoices with status matching escalation triggers
            var inPruefung = await _context.Statuses
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.InPruefung && 
                                            s.EntityType == EntityTypes.Invoice);
            var freigabeErforderlich2 = await _context.Statuses
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich && 
                                            s.EntityType == EntityTypes.Invoice);
            
            var statusIds = new List<int>();
            if (inPruefung?.Id != null) statusIds.Add(inPruefung.Id);
            if (freigabeErforderlich2?.Id != null) statusIds.Add(freigabeErforderlich2.Id);

            var invoicesToCheck = await _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.CostCenter)
                .Where(i => statusIds.Contains(i.StatusId ?? -1))
                .ToListAsync();

            foreach (var invoice in invoicesToCheck)
            {
                foreach (var rule in activeRules)
                {
                    // Check if invoice status matches any of the trigger statuses
                    var triggerStatusIds = rule.TriggerStatuses?.Select(ts => ts.StatusId).ToList() ?? new List<int>();
                    
                    if (!triggerStatusIds.Contains(invoice.StatusId ?? -1))
                        continue;

                    if (ShouldEscalate(invoice, rule))
                    {
                        
                        // Check if already escalated with this rule
                        var alreadyEscalated = await _context.EscalationLogs
                            .AnyAsync(el =>
                                el.InvoiceId == invoice.Id &&
                                el.EscalationRuleId == rule.Id &&
                                el.SentAt > DateTime.UtcNow.AddHours(-rule.RepeatIntervalHours.GetValueOrDefault(24))
                            );

                        if (!alreadyEscalated)
                        {
                            await SendEscalationEmailAsync(invoice.Id, rule.Id);
                        }
                        // Already escalated within repeat interval, skip
                    }
                }
            }
        }
        catch (Exception)
        {
            // Silently continue on error
        }
    }

    /// <summary>
    /// Ensure escalation tracking entry exists for audit purposes
    /// </summary>
    public async Task EnsureEscalationTrackingAsync(int invoiceId, int escalationRuleId)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            var rule = await _context.EscalationRules.FindAsync(escalationRuleId);

            if (invoice == null || rule == null)
                return;

            // Create tracking entry if not already exists for today
            var today = DateTime.UtcNow.Date;
            var existingLog = await _context.EscalationLogs
                .Where(el =>
                    el.InvoiceId == invoiceId &&
                    el.EscalationRuleId == escalationRuleId &&
                    el.SentAt >= today &&
                    el.SentAt < today.AddDays(1)
                )
                .FirstOrDefaultAsync();

            if (existingLog == null)
            {
                var log = new EscalationLog
                {
                    InvoiceId = invoiceId,
                    EscalationRuleId = escalationRuleId,
                    SentAt = DateTime.UtcNow,
                    Status = "Sent"
                };

                _context.EscalationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
            // Silently continue on error
        }
    }

    private bool ShouldEscalate(Invoice invoice, EscalationRule rule)
    {
        var timeInCurrentStatus = DateTime.UtcNow - invoice.UpdatedAt;
        
        // Handle edge case where UpdatedAt is in the future (shouldn't happen but be defensive)
        if (timeInCurrentStatus < TimeSpan.Zero)
            return false;
        
        var triggerTimespan = TimeSpan.FromMinutes(rule.TriggerAfterMinutes);
        return timeInCurrentStatus >= triggerTimespan;
    }

    private async Task<List<User>> GetRecipientEmailsAsync(EscalationRule rule)
    {
        var recipients = new List<User>();

        // Add specific users if configured via NotifyUsers collection
        if (rule.NotifyUsers?.Any() == true)
        {
            var notifyUserIds = rule.NotifyUsers.Select(nu => nu.UserId).ToList();
            var users = await _context.Users
                .Where(u => notifyUserIds.Contains(u.Id) && u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                .ToListAsync();
            recipients.AddRange(users);
        }

        // Add users by roles if configured via NotifyRoles collection
        if (rule.NotifyRoles?.Any() == true)
        {
            var notifyRoleIds = rule.NotifyRoles.Select(nr => nr.RoleId).ToList();
            var roleUsers = await _context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.IsActive && 
                           !string.IsNullOrWhiteSpace(u.Email) &&
                           u.UserRoles.Any(ur => notifyRoleIds.Contains(ur.RoleId)))
                .ToListAsync();
            recipients.AddRange(roleUsers);
        }

        return recipients.DistinctBy(r => r.Id).ToList();
    }

    private string GenerateEmailContent(Invoice invoice, EscalationRule rule)
    {
        var templateContent = rule.MessageTemplate ?? GetDefaultTemplate();

        // Replace template placeholders
        var appUrl = _configuration["AppSettings:ApplicationUrl"] ?? "#";

        var escalationHours = Math.Round(rule.TriggerAfterMinutes / 60.0, 1);
        var content = templateContent!
            .Replace("{{InvoiceNumber}}", invoice.InvoiceNumber ?? "")
            .Replace("{{SupplierName}}", invoice.Supplier?.Name ?? "Unknown")
            .Replace("{{Amount}}", invoice.TotalAmount.ToString("C"))
            .Replace("{{Status}}", invoice.Status?.ToString() ?? "Unknown")
            .Replace("{{CreatedDate}}", invoice.CreatedAt.ToString("yyyy-MM-dd HH:mm"))
            .Replace("{{RuleName}}", rule.Name ?? "Escalation Rule")
            .Replace("{{EscalationLevel}}", escalationHours.ToString())
            .Replace("{{CurrentTime}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"))
            .Replace("{{CompanyName}}", "Rechnungsfreigabe System")
            .Replace("{{ApplicationUrl}}", appUrl);

        return content ?? "";
    }

    private string GetDefaultTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body { font-family: Arial, sans-serif; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #d32f2f; color: white; padding: 15px; border-radius: 5px; }
        .content { padding: 20px; background-color: #f5f5f5; margin-top: 10px; border-radius: 5px; }
        .details { margin: 15px 0; }
        .detail-row { margin: 8px 0; }
        .label { font-weight: bold; display: inline-block; width: 150px; }
        .value { display: inline-block; }
        .footer { margin-top: 20px; font-size: 12px; color: #999; }
        .action-button { 
            display: inline-block; 
            padding: 10px 20px; 
            background-color: #1976d2; 
            color: white; 
            text-decoration: none; 
            border-radius: 5px; 
            margin-top: 15px;
        }
        .workflow-info {
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px;
            margin: 15px 0;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0; font-size: 18px;'>⚠️ INVOICE ESCALATION ALERT</h2>
        </div>

        <div class='content'>
            <p>Dear Approver,</p>

            <p>An invoice has been escalated and requires your immediate attention:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='label'>Invoice Number:</span>
                    <span class='value'><strong>{{InvoiceNumber}}</strong></span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Supplier:</span>
                    <span class='value'>{{SupplierName}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Amount:</span>
                    <span class='value'><strong>{{Amount}}</strong></span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Current Status:</span>
                    <span class='value'>{{Status}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Created Date:</span>
                    <span class='value'>{{CreatedDate}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Time in Status:</span>
                    <span class='value'>{{EscalationLevel}} hours</span>
                </div>
            </div>

            <div class='workflow-info'>
                <p style='margin: 5px 0;'><strong>📋 Workflow Information:</strong></p>
                <p style='margin: 5px 0;'>This invoice ({{InvoiceNumber}}) is currently in the approval workflow and has exceeded the expected processing time.</p>
                <p style='margin: 5px 0;'><strong>Escalation Reason:</strong> {{RuleName}}</p>
            </div>

            <p><strong>Action Required:</strong> Please review and approve or reject this invoice as soon as possible to keep the workflow moving.</p>

            <a href='{{ApplicationUrl}}/invoices/{{InvoiceNumber}}' class='action-button'>View Invoice Details</a>
        </div>

        <div class='footer'>
            <p>This is an automated escalation notification from {{CompanyName}}.</p>
            <p>Sent: {{CurrentTime}}</p>
            <p>If you believe you received this message in error, please contact your administrator.</p>
        </div>
    </div>
</body>
</html>";
    }

    private async Task LogEscalationAsync(int invoiceId, int escalationRuleId, int recipientId)
    {
        try
        {
            var log = new EscalationLog
            {
                InvoiceId = invoiceId,
                EscalationRuleId = escalationRuleId,
                RecipientUserId = recipientId,
                SentAt = DateTime.UtcNow,
                Status = "Sent"
            };

            _context.EscalationLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging escalation");
        }
    }
}

