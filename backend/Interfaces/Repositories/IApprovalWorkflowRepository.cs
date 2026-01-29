using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IApprovalWorkflowRepository : IRepository<ApprovalWorkflow>
{
    Task<ApprovalWorkflow?> GetByIdWithIncludesAsync(int id);
    Task<IEnumerable<ApprovalWorkflow>> GetByInvoiceIdAsync(int invoiceId);
    Task<IEnumerable<ApprovalWorkflow>> GetPendingForUserAsync(int userId);
    Task<IEnumerable<ApprovalWorkflow>> GetByStatusAsync(string statusCode);
}
