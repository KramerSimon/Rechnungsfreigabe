using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace RechnungsfreigabeAPI.Services;

public interface IEscalationEmailService
{
    Task SendEscalationEmailAsync(int invoiceId, int escalationRuleId);
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
    /// </summary>
    public async Task SendEscalationEmailAsync(int invoiceId, int escalationRuleId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.CostCenter)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found for escalation email", invoiceId);
                return;
            }

            var rule = await _context.EscalationRules
                .Include(r => r.NotifyUser)
                .FirstOrDefaultAsync(r => r.Id == escalationRuleId);

            if (rule == null)
            {
                _logger.LogWarning("Escalation rule {RuleId} not found", escalationRuleId);
                return;
            }

            if (!rule.IsActive)
            {
                _logger.LogInformation("Escalation rule {RuleId} is inactive", escalationRuleId);
                return;
            }

            // Get recipients
            var recipients = await GetRecipientEmailsAsync(rule);
            if (!recipients.Any())
            {
                _logger.LogWarning("No recipients found for escalation rule {RuleId}", escalationRuleId);
                return;
            }

            // Generate email content
            var emailContent = GenerateEmailContent(invoice, rule);
            var subject = $"ESCALATION: Invoice {invoice.InvoiceNumber} requires attention";

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

                    _logger.LogInformation(
                        "Escalation email sent to {Email} for invoice {InvoiceId} (Rule: {RuleId})",
                        recipient.Email,
                        invoiceId,
                        escalationRuleId
                    );

                    // Track the escalation
                    await LogEscalationAsync(invoiceId, escalationRuleId, recipient.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send escalation email to {Email} for invoice {InvoiceId}",
                        recipient.Email,
                        invoiceId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in SendEscalationEmailAsync for invoice {InvoiceId}, rule {RuleId}",
                invoiceId,
                escalationRuleId
            );
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
                .Where(r => r.IsActive)
                .Include(r => r.NotifyUser)
                .ToListAsync();

            if (!activeRules.Any())
            {
                _logger.LogInformation("No active escalation rules found");
                return;
            }

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
                // Filter to likely actionable states; remaining filtering happens below
                .Where(i => statusIds.Contains(i.StatusId ?? -1))
                .ToListAsync();

            foreach (var invoice in invoicesToCheck)
            {
                foreach (var rule in activeRules)
                {
                    var currentStatus = invoice.Status?.ToString() ?? string.Empty;
                    if (!rule.TriggerStatus.Equals(currentStatus, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Check if this escalation should trigger
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
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessEscalationEmailsAsync");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EnsureEscalationTrackingAsync");
        }
    }

    private bool ShouldEscalate(Invoice invoice, EscalationRule rule)
    {
        var timeInCurrentStatus = DateTime.UtcNow - invoice.UpdatedAt;
        return timeInCurrentStatus.TotalHours >= rule.TriggerAfterHours;
    }

    private async Task<List<User>> GetRecipientEmailsAsync(EscalationRule rule)
    {
        var recipients = new List<User>();

        // Add specific user if configured
        if (rule.NotifyUserId.HasValue)
        {
            var user = await _context.Users.FindAsync(rule.NotifyUserId.Value);
            if (user != null && user.IsActive && !string.IsNullOrWhiteSpace(user.Email))
            {
                recipients.Add(user);
            }
        }

        // Add users by role if configured (prefer role id, fall back to role name)
        if (rule.NotifyRoleId.HasValue || !string.IsNullOrWhiteSpace(rule.NotifyRole))
        {
            var roleUsersQuery = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email));

            if (rule.NotifyRoleId.HasValue)
            {
                roleUsersQuery = roleUsersQuery.Where(u => u.UserRoles.Any(ur => ur.RoleId == rule.NotifyRoleId));
            }
            else if (!string.IsNullOrWhiteSpace(rule.NotifyRole))
            {
                roleUsersQuery = roleUsersQuery.Where(u => u.UserRoles.Any(ur => ur.Role.Name == rule.NotifyRole));
            }

            var roleUsers = await roleUsersQuery.ToListAsync();
            recipients.AddRange(roleUsers);
        }

        // Remove duplicates
        return recipients.DistinctBy(r => r.Id).ToList();
    }

    private string GenerateEmailContent(Invoice invoice, EscalationRule rule)
    {
        var templateContent = rule.MessageTemplate ?? GetDefaultTemplate();

        // Replace template placeholders
        var appUrl = _configuration["AppSettings:ApplicationUrl"] ?? "#";

        var content = templateContent
            .Replace("{{InvoiceNumber}}", invoice.InvoiceNumber)
            .Replace("{{SupplierName}}", invoice.Supplier?.Name ?? "Unknown")
            .Replace("{{Amount}}", invoice.TotalAmount.ToString("C"))
            .Replace("{{Status}}", invoice.Status?.ToString() ?? "Unknown")
            .Replace("{{CreatedDate}}", invoice.CreatedAt.ToString("yyyy-MM-dd HH:mm"))
            .Replace("{{RuleName}}", rule.Name)
            .Replace("{{EscalationLevel}}", rule.TriggerAfterHours.ToString())
            .Replace("{{CurrentTime}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"))
            .Replace("{{CompanyName}}", "Rechnungsfreigabe System")
            .Replace("{{ApplicationUrl}}", appUrl);

        return content;
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
        .label { font-weight: bold; display: inline-block; width: 120px; }
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
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0; font-size: 18px;'>INVOICE ESCALATION ALERT</h2>
        </div>

        <div class='content'>
            <p>Dear User,</p>

            <p>An invoice has been escalated and requires your immediate attention:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='label'>Invoice Number:</span>
                    <span class='value'>{{InvoiceNumber}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Supplier:</span>
                    <span class='value'>{{SupplierName}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Amount:</span>
                    <span class='value'>{{Amount}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Current Status:</span>
                    <span class='value'>{{Status}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Created:</span>
                    <span class='value'>{{CreatedDate}}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Days Pending:</span>
                    <span class='value'>{{EscalationLevel}} hours</span>
                </div>
            </div>

            <p><strong>Escalation Reason:</strong> {{RuleName}}</p>

            <p>Please take action on this invoice as soon as possible. Click the button below to access the invoice:</p>

            <a href='{{ApplicationUrl}}/invoices/{{InvoiceNumber}}' class='action-button'>View Invoice</a>
        </div>

        <div class='footer'>
            <p>This is an automated message from {{CompanyName}}.</p>
            <p>Sent: {{CurrentTime}}</p>
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

