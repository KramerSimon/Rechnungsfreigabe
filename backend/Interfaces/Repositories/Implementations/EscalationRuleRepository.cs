using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class EscalationRuleRepository : Repository<EscalationRule>, IEscalationRuleRepository
{
    public EscalationRuleRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<EscalationRule>> GetActiveRulesAsync()
    {
        return await _dbSet
            .Where(r => r.IsActive)
            .Include(r => r.TriggerStatuses)
                .ThenInclude(ts => ts.Status)
            .Include(r => r.NotifyRoles)
                .ThenInclude(nr => nr.Role)
            .Include(r => r.NotifyUsers)
                .ThenInclude(nu => nu.User)
            .ToListAsync();
    }

    public async Task<EscalationRule?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbSet
            .Include(r => r.TriggerStatuses)
                .ThenInclude(ts => ts.Status)
            .Include(r => r.NotifyRoles)
                .ThenInclude(nr => nr.Role)
            .Include(r => r.NotifyUsers)
                .ThenInclude(nu => nu.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
