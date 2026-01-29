using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class SystemConfigRepository : Repository<SystemConfig>, ISystemConfigRepository
{
    public SystemConfigRepository(ApplicationDbContext context) : base(context) { }

    public async Task<SystemConfig?> GetByKeyAsync(string key)
    {
        return await _dbSet
            .FirstOrDefaultAsync(sc => sc.ConfigKey == key);
    }
}
