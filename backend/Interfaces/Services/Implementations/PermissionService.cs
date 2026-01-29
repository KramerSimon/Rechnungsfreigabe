using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork unitOfWork;

    public PermissionService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        var permissions = await unitOfWork.Permissions.GetAllOrderedAsync();
        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Code = p.Code,
            Category = p.Category,
            IsSystemPermission = p.IsSystemPermission
        }).ToList();
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(int id)
    {
        var permission = await unitOfWork.Permissions.GetByIdAsync(id);
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

        unitOfWork.Permissions.Add(permission);
        await unitOfWork.SaveChangesAsync();

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
        var permission = await unitOfWork.Permissions.GetByIdAsync(id);
        if (permission == null)
            throw new KeyNotFoundException($"Permission with ID {id} not found");

        if (!permission.IsSystemPermission)
        {
            if (dto.Name != null) permission.Name = dto.Name;
            if (dto.Description != null) permission.Description = dto.Description;
            if (dto.Category != null) permission.Category = dto.Category;
        }

        await unitOfWork.SaveChangesAsync();

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
        var permission = await unitOfWork.Permissions.GetByIdAsync(id);
        if (permission == null)
            throw new KeyNotFoundException($"Permission with ID {id} not found");

        if (permission.IsSystemPermission)
            throw new InvalidOperationException("System permissions cannot be deleted");

        unitOfWork.Permissions.Remove(permission);
        await unitOfWork.SaveChangesAsync();
    }
}
