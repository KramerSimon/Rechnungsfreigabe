using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IStatusRepository : IRepository<Status>
{
    Task<Status?> GetByCodeAndTypeAsync(string code, string entityType);
    Task<IEnumerable<Status>> GetByEntityTypeAsync(string entityType);
}
