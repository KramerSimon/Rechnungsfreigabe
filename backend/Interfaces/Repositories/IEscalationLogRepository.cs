using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IEscalationLogRepository : IRepository<EscalationLog>
{
    Task<bool> HasEscalationLogAsync(int invoiceId, int escalationRuleId);
    Task<EscalationLog?> GetByInvoiceAndRuleAsync(int invoiceId, int escalationRuleId);
}
