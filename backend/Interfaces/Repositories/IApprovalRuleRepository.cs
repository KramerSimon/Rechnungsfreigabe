using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IApprovalRuleRepository : IRepository<ApprovalRule>
{
    Task<IEnumerable<ApprovalRule>> GetActiveRulesAsync();
    Task<IEnumerable<ApprovalRule>> GetByPriorityAsync();
}
