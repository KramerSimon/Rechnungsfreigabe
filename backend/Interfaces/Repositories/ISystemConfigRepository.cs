using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface ISystemConfigRepository : IRepository<SystemConfig>
{
    Task<SystemConfig?> GetByKeyAsync(string key);
}
