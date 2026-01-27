using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Services;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<RoleDto?> GetByIdAsync(int id);
    Task<RoleDto> CreateAsync(CreateRoleDto dto);
    Task<RoleDto?> UpdateAsync(int id, UpdateRoleDto dto);
    Task<bool> DeleteAsync(int id);
}

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _context;

    public RoleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync();
        return roles.Select(MapToDto);
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
        return role != null ? MapToDto(role) : null;
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var role = new Role
        {
            Name = dto.Name,
            Description = dto.Description,
            Color = dto.Color ?? "#ff9800",
            IsSystemRole = dto.IsSystemRole ?? false
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Add permissions
        if (dto.Permissions != null && dto.Permissions.Any())
        {
            var permissionIds = new List<int>();
            foreach (var perm in dto.Permissions)
            {
                if (perm is int intId)
                {
                    permissionIds.Add(intId);
                }
                else if (int.TryParse(perm.ToString(), out var parsedId))
                {
                    permissionIds.Add(parsedId);
                }
            }

            foreach (var permId in permissionIds)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
            await _context.SaveChangesAsync();
        }

        // Reload with permissions
        await _context.Entry(role).Collection(r => r.RolePermissions).LoadAsync();
        return MapToDto(role);
    }

    public async Task<RoleDto?> UpdateAsync(int id, UpdateRoleDto dto)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (role == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name) && !role.IsSystemRole)
            role.Name = dto.Name;
        if (dto.Description != null)
            role.Description = dto.Description;
        if (!string.IsNullOrWhiteSpace(dto.Color))
            role.Color = dto.Color;
        if (dto.IsSystemRole.HasValue)
            role.IsSystemRole = dto.IsSystemRole.Value;

        // Update permissions
        if (dto.Permissions != null)
        {
            // Remove old permissions
            var oldPermissions = _context.RolePermissions.Where(rp => rp.RoleId == id).ToList();
            _context.RolePermissions.RemoveRange(oldPermissions);

            // Add new permissions
            var permissionIds = new List<int>();
            foreach (var perm in dto.Permissions)
            {
                if (perm is int intId)
                {
                    permissionIds.Add(intId);
                }
                else if (int.TryParse(perm.ToString(), out var parsedId))
                {
                    permissionIds.Add(parsedId);
                }
            }

            foreach (var permId in permissionIds)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
        }

        await _context.SaveChangesAsync();
        
        // Reload with permissions
        await _context.Entry(role).Collection(r => r.RolePermissions).LoadAsync();
        return MapToDto(role);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null || role.IsSystemRole) return false;

        var isInUse = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id);
        if (isInUse) return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }

    private static RoleDto MapToDto(Role role)
    {
        var permissions = role.RolePermissions?
            .Select(rp => rp.PermissionId)
            .ToArray() ?? Array.Empty<int>();

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = permissions.Cast<object>().ToList(),
            Color = role.Color ?? "#ff9800",
            IsSystemRole = role.IsSystemRole
        };
    }

    internal static bool IsSystem(string name)
    {
        var n = name?.Trim().ToLowerInvariant();
        return n == "admin" || n == "administrator";
    }
}
