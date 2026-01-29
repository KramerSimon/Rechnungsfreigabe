using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetActiveByIdAsync(int id);
    Task<bool> IsUserActiveAsync(int id);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);
    Task<User?> GetAdminUserAsync();
    Task<IEnumerable<User>> GetByRoleAsync(int roleId);
    Task<IEnumerable<User>> GetAllActiveWithRolesAsync();
    Task<IEnumerable<User>> GetAllWithRolesAsync();
    Task<User?> GetByIdWithRolesAsync(int id);
    Task<User?> GetByUsernameWithFullDetailsAsync(string username);
    Task<User?> GetByIdWithFullDetailsAsync(int id);
}
