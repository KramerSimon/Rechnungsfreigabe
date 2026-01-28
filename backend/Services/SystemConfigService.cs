using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly ApplicationDbContext context;

    public SystemConfigService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<SystemConfigDto>> GetAllAsync()
    {
        var configs = await context.SystemConfigs
            .Include(c => c.UpdatedByUser)
            .OrderBy(c => c.ConfigKey)
            .ToListAsync();

        return configs.Select(MapToDto);
    }

    public async Task<SystemConfigDto?> GetByKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var config = await context.SystemConfigs
            .Include(c => c.UpdatedByUser)
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        return config == null ? null : MapToDto(config);
    }

    public async Task<SystemConfigDto> UpsertAsync(string key, UpsertSystemConfigDto dto, int? updatedByUserId)
    {
        var normalizedKey = (key ?? dto.ConfigKey).Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            throw new ArgumentException("Config key is required");
        }

        var config = await context.SystemConfigs
            .Include(c => c.UpdatedByUser)
            .FirstOrDefaultAsync(c => c.ConfigKey == normalizedKey);

        if (config == null)
        {
            config = new SystemConfig
            {
                ConfigKey = normalizedKey,
                ConfigValue = dto.ConfigValue,
                DataType = dto.DataType ?? ConfigDataType.String,
                Description = dto.Description,
                IsEditable = dto.IsEditable ?? true,
                UpdatedBy = updatedByUserId,
                UpdatedAt = DateTime.UtcNow
            };

            context.SystemConfigs.Add(config);
            await context.SaveChangesAsync();
            await context.Entry(config).Reference(c => c.UpdatedByUser).LoadAsync();
            return MapToDto(config);
        }

        if (!config.IsEditable)
        {
            throw new InvalidOperationException($"Konfiguration '{normalizedKey}' ist nicht änderbar.");
        }

        if (dto.ConfigValue != null || dto.Description != null || dto.IsEditable.HasValue || dto.DataType.HasValue)
        {
            config.ConfigValue = dto.ConfigValue ?? config.ConfigValue;
            config.Description = dto.Description ?? config.Description;
            config.IsEditable = dto.IsEditable ?? config.IsEditable;
            config.DataType = dto.DataType ?? config.DataType;
        }

        config.UpdatedBy = updatedByUserId;
        config.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await context.Entry(config).Reference(c => c.UpdatedByUser).LoadAsync();

        return MapToDto(config);
    }

    private static SystemConfigDto MapToDto(SystemConfig config)
    {
        return new SystemConfigDto
        {
            Id = config.Id,
            ConfigKey = config.ConfigKey,
            ConfigValue = config.ConfigValue,
            DataType = config.DataType,
            Description = config.Description,
            IsEditable = config.IsEditable,
            UpdatedBy = config.UpdatedBy,
            UpdatedByName = config.UpdatedByUser != null
                ? $"{config.UpdatedByUser.FirstName} {config.UpdatedByUser.LastName}"
                : null,
            UpdatedAt = config.UpdatedAt
        };
    }
}
