using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class ApprovalRuleRepository : Repository<ApprovalRule>, IApprovalRuleRepository
{
    public ApprovalRuleRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<ApprovalRule>> GetActiveRulesAsync()
    {
        return await _dbSet
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApprovalRule>> GetByPriorityAsync()
    {
        return await _dbSet
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }
}
