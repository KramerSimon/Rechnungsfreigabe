using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<IEnumerable<Role>> GetAllWithPermissionsAsync();
    Task<Role?> GetByIdWithPermissionsAsync(int id);
}
