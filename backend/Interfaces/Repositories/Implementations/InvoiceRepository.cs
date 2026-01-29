using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Invoice?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbSet
            .Include(i => i.CostCenter)
            .ThenInclude(cc => cc!.Manager)
            .Include(i => i.Project)
            .ThenInclude(p => p!.ProjectManager)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice?> GetByIdWithFullDetailsAsync(int id)
    {
        return await _dbSet
            .Include(i => i.Status)
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Status)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public IQueryable<Invoice> GetAllWithFullDetailsQuery()
    {
        return _dbSet
            .Include(i => i.Status)
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Status)
            .AsQueryable();
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(string statusCode)
    {
        return await _dbSet
            .Include(i => i.Status)
            .Where(i => i.Status!.Code == statusCode)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetPendingAsync()
    {
        return await _dbSet
            .Include(i => i.Status)
            .Where(i => i.Status!.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich)
            .ToListAsync();
    }

    public async Task<Invoice?> GetWithRelationsAsync(int invoiceId)
    {
        return await _dbSet
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<Invoice?> GetForEscalationAsync(int invoiceId)
    {
        return await _dbSet
            .Include(i => i.CostCenter)
            .ThenInclude(cc => cc!.Manager)
            .Include(i => i.Project)
            .ThenInclude(p => p!.ProjectManager)
            .Include(i => i.Creator)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }
}
