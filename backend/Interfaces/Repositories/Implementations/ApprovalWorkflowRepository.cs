using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class ApprovalWorkflowRepository : Repository<ApprovalWorkflow>, IApprovalWorkflowRepository
{
    public ApprovalWorkflowRepository(ApplicationDbContext context) : base(context) { }

    public override async Task<IEnumerable<ApprovalWorkflow>> GetAllAsync()
    {
        return await _dbSet
            .Include(w => w.Approver)
            .Include(w => w.Invoice)
            .Include(w => w.Status)
            .ToListAsync();
    }

    public async Task<ApprovalWorkflow?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbSet
            .Include(w => w.Approver)
            .Include(w => w.Invoice)
            .Include(w => w.Status)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<IEnumerable<ApprovalWorkflow>> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _dbSet
            .Where(w => w.InvoiceId == invoiceId)
            .Include(w => w.Approver)
            .Include(w => w.Status)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApprovalWorkflow>> GetPendingForUserAsync(int userId)
    {
        return await _dbSet
            .Where(w => w.ApproverId == userId && w.Status!.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending)
            .Include(w => w.Approver)
            .Include(w => w.Invoice)
            .Include(w => w.Status)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApprovalWorkflow>> GetByStatusAsync(string statusCode)
    {
        return await _dbSet
            .Where(w => w.Status!.Code == statusCode)
            .Include(w => w.Approver)
            .ToListAsync();
    }
}
