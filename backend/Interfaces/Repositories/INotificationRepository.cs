using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<bool> ExistsWithConditionsAsync(int userId, string type, string title, string message);
    Task<Notification?> GetByIdWithIncludesAsync(int notificationId);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
}
