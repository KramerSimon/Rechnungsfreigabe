using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class EscalationLogRepository : Repository<EscalationLog>, IEscalationLogRepository
{
    public EscalationLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<bool> HasEscalationLogAsync(int invoiceId, int escalationRuleId)
    {
        return await _dbSet
            .AnyAsync(el => el.InvoiceId == invoiceId && el.EscalationRuleId == escalationRuleId);
    }

    public async Task<EscalationLog?> GetByInvoiceAndRuleAsync(int invoiceId, int escalationRuleId)
    {
        return await _dbSet
            .Include(el => el.EscalationRule)
            .Include(el => el.Invoice)
            .FirstOrDefaultAsync(el => el.InvoiceId == invoiceId && el.EscalationRuleId == escalationRuleId);
    }
}
