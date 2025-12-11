using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<NotificationDto> CreateNotificationAsync(int userId, string type, string title, string message, int? invoiceId = null, NotificationPriority priority = NotificationPriority.Normal);
    Task<bool> MarkNotificationAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task NotifyInvoiceCreatedAsync(int invoiceId);
    Task NotifyInvoiceApprovalAsync(int invoiceId, bool approved);
    Task NotifyOverdueInvoicesAsync();
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Include(n => n.Invoice)
            .ThenInclude(i => i.Supplier)
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return notifications.Select(MapToDto);
    }

    public async Task<NotificationDto> CreateNotificationAsync(int userId, string type, string title, string message, 
        int? invoiceId = null, NotificationPriority priority = NotificationPriority.Normal)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                InvoiceId = invoiceId,
                Type = type,
                Title = title,
                Message = message,
                Priority = priority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            if (invoiceId.HasValue)
            {
                notification.ActionUrl = $"/invoices/{invoiceId}";
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Reload with invoice details
            var createdNotification = await _context.Notifications
                .Include(n => n.Invoice)
                .ThenInclude(i => i.Supplier)
                .FirstAsync(n => n.Id == notification.Id);

            _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
            return MapToDto(createdNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId, int userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unreadNotifications.Count, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return false;
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task NotifyInvoiceCreatedAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.ApprovalWorkflows)
                .ThenInclude(aw => aw.Approver)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return;

            // Notify approvers
            var approvers = invoice.ApprovalWorkflows
                .Where(aw => aw.Status == ApprovalStatus.Pending)
                .Select(aw => aw.Approver)
                .Distinct()
                .ToList();

            foreach (var approver in approvers)
            {
                await CreateNotificationAsync(
                    approver.Id,
                    "invoice_approval_required",
                    "Neue Rechnung zur Freigabe",
                    $"Rechnung {invoice.InvoiceNumber} von {invoice.Supplier.Name} über {invoice.TotalAmount:C} EUR wartet auf Ihre Freigabe.",
                    invoiceId,
                    NotificationPriority.Normal
                );
            }

            // Notify accounting team
            var accountingUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Buchhaltung"))
                .ToListAsync();

            foreach (var accountingUser in accountingUsers)
            {
                await CreateNotificationAsync(
                    accountingUser.Id,
                    "invoice_received",
                    "Neue Rechnung eingegangen",
                    $"Rechnung {invoice.InvoiceNumber} von {invoice.Supplier.Name} über {invoice.TotalAmount:C} EUR ist eingegangen.",
                    invoiceId,
                    NotificationPriority.Low
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice created notifications for invoice {InvoiceId}", invoiceId);
        }
    }

    public async Task NotifyInvoiceApprovalAsync(int invoiceId, bool approved)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.Creator)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return;

            var actionText = approved ? "freigegeben" : "abgelehnt";
            var title = $"Rechnung {actionText}";
            var message = $"Rechnung {invoice.InvoiceNumber} von {invoice.Supplier.Name} über {invoice.TotalAmount:C} EUR wurde {actionText}.";
            var priority = approved ? NotificationPriority.Normal : NotificationPriority.High;

            // Notify creator if exists
            if (invoice.Creator != null)
            {
                await CreateNotificationAsync(
                    invoice.Creator.Id,
                    approved ? "invoice_approved" : "invoice_rejected",
                    title,
                    message,
                    invoiceId,
                    priority
                );
            }

            // Notify accounting team
            var accountingUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Buchhaltung"))
                .ToListAsync();

            foreach (var accountingUser in accountingUsers)
            {
                await CreateNotificationAsync(
                    accountingUser.Id,
                    approved ? "invoice_approved" : "invoice_rejected",
                    title,
                    message,
                    invoiceId,
                    priority
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice approval notifications for invoice {InvoiceId}", invoiceId);
        }
    }

    public async Task NotifyOverdueInvoicesAsync()
    {
        try
        {
            var overdueInvoices = await _context.Invoices
                .Include(i => i.Supplier)
                .Where(i => i.DueDate < DateTime.UtcNow && 
                           i.Status != InvoiceStatus.Bezahlt && 
                           i.Status != InvoiceStatus.Storniert &&
                           i.Status != InvoiceStatus.Ueberfaellig)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                // Update invoice status
                invoice.Status = InvoiceStatus.Ueberfaellig;
                invoice.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Notify relevant users about overdue invoices
            var financeUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => 
                    ur.Role.Name == "Buchhaltung" || ur.Role.Name == "Controller"))
                .ToListAsync();

            foreach (var user in financeUsers)
            {
                if (overdueInvoices.Any())
                {
                    await CreateNotificationAsync(
                        user.Id,
                        "overdue_invoices",
                        "Überfällige Rechnungen",
                        $"{overdueInvoices.Count} Rechnung(en) sind überfällig und benötigen Ihre Aufmerksamkeit.",
                        null,
                        NotificationPriority.High
                    );
                }
            }

            _logger.LogInformation("Processed {Count} overdue invoices", overdueInvoices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing overdue invoices notifications");
        }
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            IsRead = notification.IsRead,
            Priority = notification.Priority.ToString(),
            ActionUrl = notification.ActionUrl,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            Invoice = notification.Invoice != null ? new InvoiceDto
            {
                Id = notification.Invoice.Id,
                InvoiceNumber = notification.Invoice.InvoiceNumber,
                TotalAmount = notification.Invoice.TotalAmount,
                Currency = notification.Invoice.Currency,
                Status = notification.Invoice.Status.ToString(),
                Supplier = new SupplierDto
                {
                    Id = notification.Invoice.Supplier.Id,
                    Name = notification.Invoice.Supplier.Name
                }
            } : null
        };
    }
}