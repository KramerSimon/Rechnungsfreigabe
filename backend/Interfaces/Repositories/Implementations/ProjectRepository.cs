using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Project?> GetByIdWithManagerAsync(string id)
    {
        return await _dbSet
            .Include(p => p.ProjectManager)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Project>> GetAllWithManagerAsync()
    {
        return await _dbSet
            .Include(p => p.ProjectManager)
            .Include(p => p.CostCenter)
            .Include(p => p.Status)
            .ToListAsync();
    }

    public async Task<IEnumerable<Project>> GetByCostCenterAsync(string costCenterId)
    {
        return await _dbSet
            .Where(p => p.CostCenterId == costCenterId)
            .Include(p => p.ProjectManager)
            .ToListAsync();
    }
}
