using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByIdWithManagerAsync(string id);
    Task<IEnumerable<Project>> GetAllWithManagerAsync();
    Task<IEnumerable<Project>> GetByCostCenterAsync(string costCenterId);
}
