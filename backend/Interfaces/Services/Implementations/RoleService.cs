using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork unitOfWork;

    public RoleService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await unitOfWork.Roles.GetAllWithPermissionsAsync();
        return roles.Select(MapToDto);
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await unitOfWork.Roles.GetByIdWithPermissionsAsync(id);
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

        unitOfWork.Roles.Add(role);
        await unitOfWork.SaveChangesAsync();

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
                unitOfWork.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
            await unitOfWork.SaveChangesAsync();
        }

        // Reload with permissions
        role = await unitOfWork.Roles.GetByIdWithPermissionsAsync(role.Id);
        
        return MapToDto(role!);
    }

    public async Task<RoleDto?> UpdateAsync(int id, UpdateRoleDto dto)
    {
        var role = await unitOfWork.Roles.GetByIdWithPermissionsAsync(id);
        
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
            var allRolePermissions = await unitOfWork.RolePermissions.GetAllAsync();
            var oldPermissions = allRolePermissions.Where(rp => rp.RoleId == id).ToList();
            unitOfWork.RolePermissions.RemoveRange(oldPermissions);

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
                unitOfWork.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
        }

        await unitOfWork.SaveChangesAsync();
        
        // Reload with permissions
        role = await unitOfWork.Roles.GetByIdWithPermissionsAsync(role.Id);
        
        return MapToDto(role!);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await unitOfWork.Roles.GetByIdAsync(id);
        if (role == null || role.IsSystemRole) return false;

        var allUserRoles = await unitOfWork.UserRoles.GetAllAsync();
        var isInUse = allUserRoles.Any(ur => ur.RoleId == id);
        if (isInUse) return false;

        unitOfWork.Roles.Remove(role);
        await unitOfWork.SaveChangesAsync();
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
