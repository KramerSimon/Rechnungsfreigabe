using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserEntityByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<bool> DeleteUserAsync(int id);
    Task<string[]> GetUserPermissionsAsync(int userId);
    Task<PagedResult<UserDto>> GetUsersPagedAsync(PageRequest pageRequest);
    Task IncrementFailedLoginAttemptsAsync(int userId);
    Task ResetFailedLoginAttemptsAsync(int userId);
    Task LockUserAccountAsync(int userId, DateTime lockedUntil);
    Task UpdatePasswordAsync(int userId, string passwordHash);
    Task UpdateLastLoginAsync(int userId);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    public UserService(ApplicationDbContext context)
    {
        _context = context;
        }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .ToListAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetUserEntityByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            var user = new User
            {
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                ActiveDirectorySid = createUserDto.ActiveDirectorySid,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Add user roles
            foreach (var roleId in createUserDto.RoleIds)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);
            }

            await _context.SaveChangesAsync();

            // Reload user with roles
            var createdUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == user.Id);

            return MapToDto(createdUser);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            // Update user properties
            if (!string.IsNullOrEmpty(updateUserDto.Email))
                user.Email = updateUserDto.Email;
            if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                user.FirstName = updateUserDto.FirstName;
            if (!string.IsNullOrEmpty(updateUserDto.LastName))
                user.LastName = updateUserDto.LastName;
            if (updateUserDto.IsActive.HasValue)
                user.IsActive = updateUserDto.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;

            // Update user roles if provided
            if (updateUserDto.RoleIds != null)
            {
                // Remove existing roles
                _context.UserRoles.RemoveRange(user.UserRoles);

                // Add new roles
                foreach (var roleId in updateUserDto.RoleIds)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                }
            }

            await _context.SaveChangesAsync();

            // Reload user with updated roles
            var updatedUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == id);

            return MapToDto(updatedUser);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            // Soft delete - just mark as inactive
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<string[]> GetUserPermissionsAsync(int userId)
    {
        var rolePermissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        return rolePermissions.ToArray();
    }

    public async Task<PagedResult<UserDto>> GetUsersPagedAsync(PageRequest pageRequest)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(pageRequest.SearchTerm))
        {
            var searchTerm = pageRequest.SearchTerm.ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm));
        }

        // Apply sorting
        query = pageRequest.SortBy?.ToLower() switch
        {
            "username" => pageRequest.SortDescending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "email" => pageRequest.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "firstname" => pageRequest.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "lastname" => pageRequest.SortDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
            "createdat" => pageRequest.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => query.OrderBy(u => u.Username)
        };

        var totalCount = await query.CountAsync();
        
        var users = await query
            .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageRequest.PageNumber,
            PageSize = pageRequest.PageSize
        };
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin,
            Roles = user.UserRoles.Select(ur => new RoleDto
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description,
                Permissions = ur.Role.RolePermissions?.Select(rp => (object)rp.PermissionId).ToList() ?? new List<object>(),
                Color = ur.Role.Color,
                IsSystemRole = ur.Role.IsSystemRole
            }).ToArray()
        };
    }

    public async Task IncrementFailedLoginAttemptsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.FailedLoginAttempts++;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ResetFailedLoginAttemptsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task LockUserAccountAsync(int userId, DateTime lockedUntil)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LockedUntil = lockedUntil;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdatePasswordAsync(int userId, string passwordHash)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.PasswordHash = passwordHash;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}