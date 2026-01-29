using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PurchaseOrder?> GetByNumberAsync(string poNumber)
    {
        return await _dbSet
            .Include(po => po.Status)
            .FirstOrDefaultAsync(po => po.Id == poNumber);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByProjectIdAsync(string projectId)
    {
        return await _dbSet
            .Where(po => po.ProjectId == projectId)
            .Include(po => po.Status)
            .Include(po => po.Project)
            .ToListAsync();
    }

    public async Task<bool> ExistsByNumberAsync(string poNumber)
    {
        return await _dbSet
            .AnyAsync(po => po.Id == poNumber);
    }
}
