using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;


namespace RechnungsfreigabeAPI.Services.Interfaces;

public interface ISystemConfigService
{
    Task<IEnumerable<SystemConfigDto>> GetAllAsync();
    Task<SystemConfigDto?> GetByKeyAsync(string key);
    Task<SystemConfigDto> UpsertAsync(string key, UpsertSystemConfigDto dto, int? updatedByUserId);
}
