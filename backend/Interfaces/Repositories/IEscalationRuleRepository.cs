using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IEscalationRuleRepository : IRepository<EscalationRule>
{
    Task<IEnumerable<EscalationRule>> GetActiveRulesAsync();
    Task<EscalationRule?> GetByIdWithIncludesAsync(int id);
}
