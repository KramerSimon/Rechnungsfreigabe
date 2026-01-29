using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IUserRoleRepository : IRepository<UserRole>
{
    Task<string[]> GetUserPermissionsAsync(int userId);
}
