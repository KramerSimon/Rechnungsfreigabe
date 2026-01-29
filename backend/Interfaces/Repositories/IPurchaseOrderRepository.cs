using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetByNumberAsync(string poNumber);
    Task<IEnumerable<PurchaseOrder>> GetByProjectIdAsync(string projectId);
    Task<bool> ExistsByNumberAsync(string poNumber);
}
