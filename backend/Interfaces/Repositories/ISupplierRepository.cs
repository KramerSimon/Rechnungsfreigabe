using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<Supplier?> GetByNameAsync(string name);
    Task<IEnumerable<Supplier>> GetActiveAsync();
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
}
