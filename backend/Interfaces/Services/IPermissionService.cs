using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;


namespace RechnungsfreigabeAPI.Interfaces.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<PermissionDto?> GetPermissionByIdAsync(int id);
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionDto dto);
    Task<PermissionDto> UpdatePermissionAsync(int id, UpdatePermissionDto dto);
    Task DeletePermissionAsync(int id);
}
