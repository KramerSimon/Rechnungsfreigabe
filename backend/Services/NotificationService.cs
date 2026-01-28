using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Repositories.Interfaces;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork unitOfWork;
    public NotificationService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
        }

    /// <summary>
    /// Ensure the approver has a pending approval notification for a given invoice.
    /// Duplicate-safe and includes supplier details.
    /// </summary>
    public async Task EnsureApprovalNotificationForApproverAsync(int invoiceId, int approverId)
    {
        try
        {
            var exists = await unitOfWork.Notifications.Query().AnyAsync(n =>
                n.UserId == approverId && n.InvoiceId == invoiceId && n.Type == "invoice_approval_required");

            if (exists) return;

            var invoice = await unitOfWork.Invoices.Query()
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null || invoice.Supplier == null) return;

            await CreateNotificationAsync(
                approverId,
                "invoice_approval_required",
                "Neue Rechnung zur Freigabe",
                $"Rechnung {invoice.InvoiceNumber} von {invoice.Supplier.Name} über {invoice.TotalAmount:C} EUR wartet auf Ihre Freigabe.",
                invoiceId,
                NotificationPriority.Normal
            );
        }
        catch (Exception)
        {
            
        }
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = unitOfWork.Notifications.Query()
            .Include(n => n.Invoice)
            .ThenInclude(i => i!.Supplier)
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

            unitOfWork.Notifications.Add(notification);
            await unitOfWork.SaveChangesAsync();

            // Reload with invoice details
            var createdNotification = await unitOfWork.Notifications.Query()
                .Include(n => n.Invoice)
                .ThenInclude(i => i!.Supplier)
                .FirstAsync(n => n.Id == notification.Id);

            return MapToDto(createdNotification);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId, int userId)
    {
        try
        {
            var notification = await unitOfWork.Notifications.Query()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        try
        {
            var unreadNotifications = await unitOfWork.Notifications.Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await unitOfWork.SaveChangesAsync();
            
            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await unitOfWork.Notifications.Query()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task NotifyInvoiceCreatedAsync(int invoiceId)
    {
        try
        {
            var invoice = await unitOfWork.Invoices.Query()
                .Include(i => i.Supplier)
                .Include(i => i.ApprovalWorkflows)
                .ThenInclude(aw => aw.Approver)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return;

            var notifiedUserIds = new HashSet<int>();

            // Notify approvers
            var pendingStatus = await unitOfWork.Statuses.Query()
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending && 
                                            s.EntityType == EntityTypes.ApprovalWorkflow);
            
            var approvers = invoice.ApprovalWorkflows
                .Where(aw => aw.StatusId == pendingStatus?.Id)
                .Select(aw => aw.Approver)
                .Distinct()
                .ToList();

            foreach (var approver in approvers)
            {
                notifiedUserIds.Add(approver.Id);
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
            var accountingUsers = await unitOfWork.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Buchhaltung"))
                .ToListAsync();

            foreach (var accountingUser in accountingUsers)
            {
                notifiedUserIds.Add(accountingUser.Id);
                await CreateNotificationAsync(
                    accountingUser.Id,
                    "invoice_received",
                    "Neue Rechnung eingegangen",
                    $"Rechnung {invoice.InvoiceNumber} von {invoice.Supplier.Name} über {invoice.TotalAmount:C} EUR ist eingegangen.",
                    invoiceId,
                    NotificationPriority.Low
                );
            }

            // Notify admins (if not already notified)
            var adminUsers = await unitOfWork.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Administrator"))
                .ToListAsync();

            foreach (var adminUser in adminUsers)
            {
                if (!notifiedUserIds.Contains(adminUser.Id))
                {
                    await CreateNotificationAsync(
                        adminUser.Id,
                        "invoice_received",
                        "Neue Rechnung eingegangen",
                        $"Rechnung {invoice.InvoiceNumber} von {invoice.Supplier.Name} über {invoice.TotalAmount:C} EUR ist eingegangen.",
                        invoiceId,
                        NotificationPriority.Normal
                    );
                }
            }
        }
        catch (Exception)
        {
            
        }
    }

    public async Task NotifyInvoiceApprovalAsync(int invoiceId, bool approved)
    {
        try
        {
            var invoice = await unitOfWork.Invoices.Query()
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
            var accountingUsers = await unitOfWork.Users.Query()
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
        catch (Exception)
        {
            
        }
    }

    public async Task NotifyOverdueInvoicesAsync()
    {
        try
        {
            var bezahltStatus = await unitOfWork.Statuses.Query()
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Bezahlt && 
                                            s.EntityType == EntityTypes.Invoice);
            var storniert = await unitOfWork.Statuses.Query()
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Storniert && 
                                            s.EntityType == EntityTypes.Invoice);
            var ueberfaellig = await unitOfWork.Statuses.Query()
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Ueberfaellig && 
                                            s.EntityType == EntityTypes.Invoice);
            
            var overdueInvoices = await unitOfWork.Invoices.Query()
                .Include(i => i.Supplier)
                .Where(i => i.DueDate < DateTime.UtcNow && 
                           i.StatusId != bezahltStatus!.Id && 
                           i.StatusId != storniert!.Id &&
                           i.StatusId != ueberfaellig!.Id)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                // Update invoice status
                if (ueberfaellig != null)
                {
                    invoice.StatusId = ueberfaellig.Id;
                }
                invoice.UpdatedAt = DateTime.UtcNow;
            }

            await unitOfWork.SaveChangesAsync();

            // Notify relevant users about overdue invoices
            var financeUsers = await unitOfWork.Users.Query()
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

        }
        catch (Exception)
        {
            
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
                Status = notification.Invoice.Status?.ToString() ?? string.Empty,
                Supplier = notification.Invoice.Supplier != null ? new SupplierDto
                {
                    Id = notification.Invoice.Supplier.Id,
                    Name = notification.Invoice.Supplier.Name
                } : new SupplierDto { Id = 0, Name = string.Empty }
            } : null
        };
    }

    public async Task NotifyApprovalStatusAsync(int invoiceId, string status)
    {
        try
        {
            var invoice = await unitOfWork.Invoices.Query()
                .Include(i => i.Creator)
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return;

            var title = status == "approved" ? "Rechnung genehmigt" : "Rechnung abgelehnt";
            var message = $"Rechnung {invoice.InvoiceNumber} von {invoice.Supplier?.Name ?? "Supplier"} wurde {(status == "approved" ? "genehmigt" : "abgelehnt")}";
            var priority = status == "approved" ? NotificationPriority.Normal : NotificationPriority.High;

            if (invoice.CreatedBy.HasValue)
            {
                await CreateNotificationAsync(
                    invoice.CreatedBy.Value,
                    status == "approved" ? "approval_status" : "rejection_status",
                    title,
                    message,
                    invoiceId,
                    priority
                );
            }
        }
        catch (Exception)
        {
            
        }
    }
}