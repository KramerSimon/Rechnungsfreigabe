using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<IEnumerable<Permission>> GetAllOrderedAsync();
}
