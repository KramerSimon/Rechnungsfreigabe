using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetActiveByIdAsync(int id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<bool> IsUserActiveAsync(int id)
    {
        return await _dbSet
            .AnyAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
    {
        return await _dbSet
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName) && u.IsActive)
            .ToListAsync();
    }

    public async Task<User?> GetAdminUserAsync()
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Administrator"));
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(int roleId)
    {
        return await _dbSet
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllActiveWithRolesAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllWithRolesAsync()
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();
    }

    public async Task<User?> GetByIdWithRolesAsync(int id)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameWithFullDetailsAsync(string username)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetByIdWithFullDetailsAsync(int id)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
