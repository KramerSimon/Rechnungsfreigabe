using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class CostCenterRepository : Repository<CostCenter>, ICostCenterRepository
{
    public CostCenterRepository(ApplicationDbContext context) : base(context) { }

    public async Task<CostCenter?> GetByIdWithManagerAsync(string id)
    {
        return await _dbSet
            .Include(cc => cc.Manager)
            .FirstOrDefaultAsync(cc => cc.Id == id);
    }

    public async Task<IEnumerable<CostCenter>> GetAllWithManagerAsync()
    {
        return await _dbSet
            .Include(cc => cc.Manager)
            .ToListAsync();
    }

    public async Task<bool> HasRelatedInvoicesAsync(string id)
    {
        return await _context.Set<Invoice>()
            .AnyAsync(i => i.CostCenterId == id);
    }

    public async Task<bool> HasRelatedProjectsAsync(string id)
    {
        return await _context.Set<Project>()
            .AnyAsync(p => p.CostCenterId == id);
    }
}
