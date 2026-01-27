using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.Text.Json;

namespace RechnungsfreigabeAPI.Services;

public interface IInvoiceHistoryService
{
    Task CreateHistoryEntryAsync(CreateHistoryEntryDto createHistoryDto);
    Task<List<InvoiceHistoryDto>> GetInvoiceHistoryAsync(int invoiceId);
    Task<List<InvoiceHistoryTimelineDto>> GetInvoiceHistoryTimelineAsync(int invoiceId);
    Task CreateSystemActionAsync(int invoiceId, string action, HistoryActionType actionType, string? systemReason = null, string? policyReference = null);
    Task CreateEscalationAsync(int invoiceId, string reason, int? escalatedTo = null);
    Task CreateStatusChangeAsync(int invoiceId, string oldStatus, string newStatus, int changedBy, string? comments = null);
    Task CreateDataCompletionAsync(int invoiceId, List<FieldChangeDto> fieldChanges, int changedBy);
    Task CreateApprovalActionAsync(int invoiceId, bool approved, int approverId, string? comments = null);
    Task CreateAssignmentAsync(int invoiceId, int assignedTo, string policyReference, int? assignedBy = null);
}

public class InvoiceHistoryService : IInvoiceHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public InvoiceHistoryService(
        ApplicationDbContext context,
        IMapper mapper,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _context = context;
        _mapper = mapper;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task CreateHistoryEntryAsync(CreateHistoryEntryDto createHistoryDto)
    {
        var fieldChangesJson = createHistoryDto.FieldChanges?.Any() == true 
            ? JsonSerializer.Serialize(createHistoryDto.FieldChanges.ToDictionary(fc => fc.FieldName, fc => new { fc.OldValue, fc.NewValue, fc.DisplayName }))
            : null;

        var actionType = Enum.Parse<HistoryActionType>(createHistoryDto.ActionType);
        var actionSource = Enum.Parse<HistoryActionSource>(createHistoryDto.ActionSource);

        var history = new InvoiceHistory
        {
            InvoiceId = createHistoryDto.InvoiceId,
            Action = createHistoryDto.Action,
            ActionType = actionType,
            ActionSource = actionSource,
            OldStatus = TruncateStatus(createHistoryDto.OldStatus),
            NewStatus = TruncateStatus(createHistoryDto.NewStatus),
            FieldChanges = fieldChangesJson,
            Comments = createHistoryDto.Comments,
            PolicyReference = createHistoryDto.PolicyReference,
            SystemReason = createHistoryDto.SystemReason,
            ChangedBy = createHistoryDto.ChangedBy,
            ImportChannel = createHistoryDto.ImportChannel,
            ChangedAt = DateTime.UtcNow
        };

        _context.InvoiceHistories.Add(history);
        await _context.SaveChangesAsync();

    }

    public async Task<List<InvoiceHistoryDto>> GetInvoiceHistoryAsync(int invoiceId)
    {
        var histories = await _context.InvoiceHistories
            .Include(h => h.ChangedByUser)
            .Where(h => h.InvoiceId == invoiceId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

        return _mapper.Map<List<InvoiceHistoryDto>>(histories);
    }

    public async Task<List<InvoiceHistoryTimelineDto>> GetInvoiceHistoryTimelineAsync(int invoiceId)
    {
        var histories = await GetInvoiceHistoryAsync(invoiceId);
        
        var timeline = histories
            .GroupBy(h => GetTimelineDate(h.ChangedAt))
            .OrderByDescending(g => GetSortableDate(g.Key))
            .Select(g => new InvoiceHistoryTimelineDto
            {
                Date = g.Key,
                Entries = g.OrderByDescending(h => h.ChangedAt).ToList()
            })
            .ToList();

        return timeline;
    }

    public async Task CreateSystemActionAsync(int invoiceId, string action, HistoryActionType actionType, string? systemReason = null, string? policyReference = null)
    {
        await CreateHistoryEntryAsync(new CreateHistoryEntryDto
        {
            InvoiceId = invoiceId,
            Action = action,
            ActionType = actionType.ToString(),
            ActionSource = HistoryActionSource.System.ToString(),
            SystemReason = systemReason,
            PolicyReference = policyReference,
            ChangedBy = null // System action
        });
    }

    public async Task CreateEscalationAsync(int invoiceId, string reason, int? escalatedTo = null)
    {
        var action = escalatedTo.HasValue ? $"Eskalation an Benutzer {escalatedTo}" : "Eskalationsbenachrichtigung gesendet";
        
        await CreateHistoryEntryAsync(new CreateHistoryEntryDto
        {
            InvoiceId = invoiceId,
            Action = action,
            ActionType = HistoryActionType.Escalated.ToString(),
            ActionSource = HistoryActionSource.Escalation.ToString(),
            SystemReason = reason,
            ChangedBy = null // System escalation
        });

        // Determine target user for notification/email
        var invoice = await _context.Invoices
            .Include(i => i.CostCenter)
                .ThenInclude(cc => cc!.Manager)
            .Include(i => i.Project)
                .ThenInclude(p => p!.ProjectManager)
            .Include(i => i.Creator)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        var targetUser = escalatedTo.HasValue
            ? await _context.Users.FirstOrDefaultAsync(u => u.Id == escalatedTo.Value)
            : invoice?.CostCenter?.Manager ?? invoice?.Project?.ProjectManager ?? invoice?.Creator;

        if (targetUser != null)
        {
            var subject = $"Eskalation für Rechnung {invoice?.InvoiceNumber ?? invoiceId.ToString()}";
            var body = reason ?? "Eine Eskalation wurde ausgelöst.";

            // Send email if we have an address
            if (!string.IsNullOrWhiteSpace(targetUser.Email))
            {
                await _emailService.SendEmailAsync(targetUser.Email, subject, body, isHtml: false);
            }

            // Also create an in-app notification
            await _notificationService.CreateNotificationAsync(
                targetUser.Id,
                "invoice_escalation",
                subject,
                body,
                invoiceId,
                NotificationPriority.High);
        }
    }

    public async Task CreateStatusChangeAsync(int invoiceId, string oldStatus, string newStatus, int changedBy, string? comments = null)
    {
        await CreateHistoryEntryAsync(new CreateHistoryEntryDto
        {
            InvoiceId = invoiceId,
            Action = "Status geändert",
            ActionType = HistoryActionType.StatusChanged.ToString(),
            ActionSource = HistoryActionSource.User.ToString(),
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comments = comments,
            ChangedBy = changedBy
        });
    }

    public async Task CreateDataCompletionAsync(int invoiceId, List<FieldChangeDto> fieldChanges, int changedBy)
    {
        await CreateHistoryEntryAsync(new CreateHistoryEntryDto
        {
            InvoiceId = invoiceId,
            Action = "Daten vervollständigt",
            ActionType = HistoryActionType.DataCompleted.ToString(),
            ActionSource = HistoryActionSource.User.ToString(),
            FieldChanges = fieldChanges,
            ChangedBy = changedBy
        });
    }

    public async Task CreateApprovalActionAsync(int invoiceId, bool approved, int approverId, string? comments = null)
    {
        var action = approved ? "Freigabe erteilt" : "Freigabe abgelehnt";
        var actionType = approved ? HistoryActionType.Approved : HistoryActionType.Rejected;
        var newStatus = approved ? "FREIGEGEBEN" : "ABGELEHNT";

        await CreateHistoryEntryAsync(new CreateHistoryEntryDto
        {
            InvoiceId = invoiceId,
            Action = action,
            ActionType = actionType.ToString(),
            ActionSource = HistoryActionSource.User.ToString(),
            NewStatus = newStatus,
            Comments = comments,
            ChangedBy = approverId
        });
    }

    public async Task CreateAssignmentAsync(int invoiceId, int assignedTo, string policyReference, int? assignedBy = null)
    {
        var actionSource = assignedBy.HasValue ? HistoryActionSource.User : HistoryActionSource.Policy;
        
        await CreateHistoryEntryAsync(new CreateHistoryEntryDto
        {
            InvoiceId = invoiceId,
            Action = "Zuweisung zur Freigabe",
            ActionType = HistoryActionType.Assigned.ToString(),
            ActionSource = actionSource.ToString(),
            PolicyReference = policyReference,
            ChangedBy = assignedBy
        });
    }

    private static string GetTimelineDate(DateTime date)
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        
        if (date.Date == today)
            return "HEUTE";
        
        if (date.Date == yesterday)
            return "GESTERN";
        
        return date.ToString("dd. MMMM yyyy", new System.Globalization.CultureInfo("de-DE")).ToUpper();
    }

    private static DateTime GetSortableDate(string timelineDate)
    {
        return timelineDate switch
        {
            "HEUTE" => DateTime.Today,
            "GESTERN" => DateTime.Today.AddDays(-1),
            _ => DateTime.ParseExact(timelineDate, "dd. MMMM yyyy", new System.Globalization.CultureInfo("de-DE"))
        };
    }

    private static string? TruncateStatus(string? status)
    {
        // Truncate status to 20 characters to fit database column
        if (status == null) return null;
        return status.Length > 20 ? status.Substring(0, 20) : status;
    }
}