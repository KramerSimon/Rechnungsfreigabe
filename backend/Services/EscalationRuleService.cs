using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Services;

public interface IEscalationRuleService
{
    Task<IEnumerable<EscalationRuleDto>> GetAllAsync();
    Task<EscalationRuleDto?> GetByIdAsync(int id);
    Task<EscalationRuleDto> CreateAsync(CreateEscalationRuleDto dto);
    Task<EscalationRuleDto?> UpdateAsync(int id, UpdateEscalationRuleDto dto);
    Task<bool> DeleteAsync(int id);
}

public class EscalationRuleService : IEscalationRuleService
{
    private readonly ApplicationDbContext _context;
    public EscalationRuleService(ApplicationDbContext context)
    {
        _context = context;
        }

    public async Task<IEnumerable<EscalationRuleDto>> GetAllAsync()
    {
        var rules = await _context.EscalationRules
            .Include(r => r.NotifyUser)
            .Include(r => r.NotifyRoleRef)
            .OrderBy(r => r.TriggerAfterHours)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return rules.Select(MapToDto);
    }

    public async Task<EscalationRuleDto?> GetByIdAsync(int id)
    {
        var rule = await _context.EscalationRules
            .Include(r => r.NotifyUser)
            .Include(r => r.NotifyRoleRef)
            .FirstOrDefaultAsync(r => r.Id == id);

        return rule == null ? null : MapToDto(rule);
    }

    public async Task<EscalationRuleDto> CreateAsync(CreateEscalationRuleDto dto)
    {
        var rule = new EscalationRule
        {
            Name = dto.Name,
            Description = dto.Description,
            TriggerStatus = dto.TriggerStatus,
            TriggerAfterHours = dto.TriggerAfterHours,
            RepeatIntervalHours = dto.RepeatIntervalHours,
            MaxEscalations = dto.MaxEscalations,
            NotifyRole = dto.NotifyRole,
            NotifyRoleId = dto.NotifyRoleId,
            NotifyUserId = dto.NotifyUserId,
            MessageTemplate = dto.MessageTemplate,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dto.NotifyRoleId.HasValue)
        {
            var roleName = await _context.Roles
                .Where(r => r.Id == dto.NotifyRoleId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                rule.NotifyRole = roleName;
            }
        }

        _context.EscalationRules.Add(rule);
        await _context.SaveChangesAsync();

        // reload with navigation property
        var created = await _context.EscalationRules
            .Include(r => r.NotifyUser)
            .Include(r => r.NotifyRoleRef)
            .FirstAsync(r => r.Id == rule.Id);

        return MapToDto(created);
    }

    public async Task<EscalationRuleDto?> UpdateAsync(int id, UpdateEscalationRuleDto dto)
    {
        var rule = await _context.EscalationRules.FindAsync(id);
        if (rule == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name)) rule.Name = dto.Name;
        if (dto.Description != null) rule.Description = dto.Description;
        if (!string.IsNullOrWhiteSpace(dto.TriggerStatus)) rule.TriggerStatus = dto.TriggerStatus;
        if (dto.TriggerAfterHours.HasValue) rule.TriggerAfterHours = dto.TriggerAfterHours.Value;
        if (dto.RepeatIntervalHours.HasValue) rule.RepeatIntervalHours = dto.RepeatIntervalHours.Value;
        if (dto.MaxEscalations.HasValue) rule.MaxEscalations = dto.MaxEscalations.Value;
        if (dto.NotifyRole != null) rule.NotifyRole = dto.NotifyRole;
        if (dto.NotifyRoleId.HasValue)
        {
            rule.NotifyRoleId = dto.NotifyRoleId.Value;
            var roleNameUpdated = await _context.Roles
                .Where(r => r.Id == dto.NotifyRoleId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(roleNameUpdated))
            {
                rule.NotifyRole = roleNameUpdated;
            }
        }
        if (dto.NotifyUserId.HasValue) rule.NotifyUserId = dto.NotifyUserId.Value;
        if (dto.MessageTemplate != null) rule.MessageTemplate = dto.MessageTemplate;
        if (dto.IsActive.HasValue) rule.IsActive = dto.IsActive.Value;

        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var updated = await _context.EscalationRules
            .Include(r => r.NotifyUser)
            .Include(r => r.NotifyRoleRef)
            .FirstAsync(r => r.Id == id);

        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rule = await _context.EscalationRules.FindAsync(id);
        if (rule == null) return false;

        _context.EscalationRules.Remove(rule);
        await _context.SaveChangesAsync();

        return true;
    }

    private static EscalationRuleDto MapToDto(EscalationRule rule)
    {
        return new EscalationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            TriggerStatus = rule.TriggerStatus,
            TriggerAfterHours = rule.TriggerAfterHours,
            RepeatIntervalHours = rule.RepeatIntervalHours,
            MaxEscalations = rule.MaxEscalations,
            NotifyRole = rule.NotifyRole,
            NotifyRoleId = rule.NotifyRoleId,
            NotifyRoleName = rule.NotifyRoleRef?.Name ?? rule.NotifyRole,
            NotifyUserId = rule.NotifyUserId,
            NotifyUserName = rule.NotifyUser != null ? $"{rule.NotifyUser.FirstName} {rule.NotifyUser.LastName}" : null,
            MessageTemplate = rule.MessageTemplate,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}
