using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<PermissionDto?> GetPermissionByIdAsync(int id);
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionDto dto);
    Task<PermissionDto> UpdatePermissionAsync(int id, UpdatePermissionDto dto);
    Task DeletePermissionAsync(int id);
}

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Code = p.Code,
                Category = p.Category,
                IsSystemPermission = p.IsSystemPermission
            })
            .ToListAsync();
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(int id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null) return null;

        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Code = permission.Code,
            Category = permission.Category,
            IsSystemPermission = permission.IsSystemPermission
        };
    }

    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionDto dto)
    {
        var permission = new Permission
        {
            Name = dto.Name,
            Description = dto.Description,
            Code = dto.Code,
            Category = dto.Category,
            IsSystemPermission = dto.IsSystemPermission
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Code = permission.Code,
            Category = permission.Category,
            IsSystemPermission = permission.IsSystemPermission
        };
    }

    public async Task<PermissionDto> UpdatePermissionAsync(int id, UpdatePermissionDto dto)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
            throw new KeyNotFoundException($"Permission with ID {id} not found");

        if (!permission.IsSystemPermission)
        {
            if (dto.Name != null) permission.Name = dto.Name;
            if (dto.Description != null) permission.Description = dto.Description;
            if (dto.Category != null) permission.Category = dto.Category;
        }

        await _context.SaveChangesAsync();

        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Code = permission.Code,
            Category = permission.Category,
            IsSystemPermission = permission.IsSystemPermission
        };
    }

    public async Task DeletePermissionAsync(int id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
            throw new KeyNotFoundException($"Permission with ID {id} not found");

        if (permission.IsSystemPermission)
            throw new InvalidOperationException("System permissions cannot be deleted");

        _context.Permissions.Remove(permission);
        await _context.SaveChangesAsync();
    }
}
