using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;


namespace RechnungsfreigabeAPI.Services.Interfaces;

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
    Task NotifyApprovalStatusAsync(int invoiceId, string status);
    Task EnsureApprovalNotificationForApproverAsync(int invoiceId, int approverId);
}
