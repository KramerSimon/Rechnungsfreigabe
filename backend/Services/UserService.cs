using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Repositories.Interfaces;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IPasswordService passwordService;
    public UserService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        this.unitOfWork = unitOfWork;
        this.passwordService = passwordService;
        }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .ToListAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await unitOfWork.Users.Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetUserEntityByIdAsync(int id)
    {
        return await unitOfWork.Users.Query()
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
            // Hash the password
            var passwordHash = passwordService.HashPassword(createUserDto.Password);

            var user = new User
            {
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                ActiveDirectorySid = createUserDto.ActiveDirectorySid,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            unitOfWork.Users.Add(user);
            await unitOfWork.SaveChangesAsync();

            // Add user roles
            foreach (var roleId in createUserDto.RoleIds)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                };
                unitOfWork.UserRoles.Add(userRole);
            }

            await unitOfWork.SaveChangesAsync();

            // Reload user with roles
            var createdUser = await unitOfWork.Users.Query()
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
            var user = await unitOfWork.Users.Query()
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
                unitOfWork.UserRoles.RemoveRange(user.UserRoles);

                // Add new roles
                foreach (var roleId in updateUserDto.RoleIds)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    };
                    unitOfWork.UserRoles.Add(userRole);
                }
            }

            await unitOfWork.SaveChangesAsync();

            // Reload user with updated roles
            var updatedUser = await unitOfWork.Users.Query()
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
            var user = await unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            // Soft delete - just mark as inactive
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<string[]> GetUserPermissionsAsync(int userId)
    {
        var rolePermissions = await unitOfWork.UserRoles.Query()
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
        var query = unitOfWork.Users.Query()
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
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            user.FailedLoginAttempts++;
            user.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task ResetFailedLoginAttemptsAsync(int userId)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task LockUserAccountAsync(int userId, DateTime lockedUntil)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            user.LockedUntil = lockedUntil;
            user.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task UpdatePasswordAsync(int userId, string passwordHash)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            user.PasswordHash = passwordHash;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }
    }
}