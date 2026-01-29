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
            .ToListAsync();
    }

    public async Task<EscalationRule?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbSet
            .Include(r => r.TriggerStatuses)
            .Include(r => r.NotifyRoles)
            .Include(r => r.NotifyUsers)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
