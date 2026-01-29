using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<bool> ExistsWithConditionsAsync(int userId, string type, string title, string message)
    {
        return await _dbSet
            .AnyAsync(n => n.UserId == userId && n.Type == type && n.Title == title && n.Message == message);
    }

    public async Task<Notification?> GetByIdWithIncludesAsync(int notificationId)
    {
        return await _dbSet
            .Include(n => n.Invoice)
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == notificationId);
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _dbSet
            .Where(n => n.UserId == userId);
            
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }
            
        return await query
            .Include(n => n.Invoice)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}
