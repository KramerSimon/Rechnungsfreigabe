using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface ICostCenterRepository : IRepository<CostCenter>
{
    Task<CostCenter?> GetByIdWithManagerAsync(string id);
    Task<bool> HasRelatedInvoicesAsync(string id);
    Task<bool> HasRelatedProjectsAsync(string id);
}
