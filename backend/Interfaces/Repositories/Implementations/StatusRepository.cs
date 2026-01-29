using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class StatusRepository : Repository<Status>, IStatusRepository
{
    public StatusRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Status?> GetByCodeAndTypeAsync(string code, string entityType)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Code == code && s.EntityType == entityType);
    }

    public async Task<IEnumerable<Status>> GetByEntityTypeAsync(string entityType)
    {
        return await _dbSet
            .Where(s => s.EntityType == entityType)
            .ToListAsync();
    }
}
