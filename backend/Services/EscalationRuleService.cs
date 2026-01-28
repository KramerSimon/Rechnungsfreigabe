using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Repositories.Interfaces;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Services;

public class EscalationRuleService : IEscalationRuleService
{
    private readonly IUnitOfWork unitOfWork;
    
    public EscalationRuleService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EscalationRuleDto>> GetAllAsync()
    {
        var rules = await unitOfWork.EscalationRules.Query()
            .Include(r => r.TriggerStatuses)
            .ThenInclude(ts => ts.Status)
            .Include(r => r.NotifyRoles)
            .ThenInclude(nr => nr.Role)
            .Include(r => r.NotifyUsers)
            .ThenInclude(nu => nu.User)
            .OrderBy(r => r.TriggerAfterMinutes)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return rules.Select(r => MapToDto(r));
    }

    public async Task<EscalationRuleDto?> GetByIdAsync(int id)
    {
        var rule = await unitOfWork.EscalationRules.Query()
            .Include(r => r.TriggerStatuses)
            .ThenInclude(ts => ts.Status)
            .Include(r => r.NotifyRoles)
            .ThenInclude(nr => nr.Role)
            .Include(r => r.NotifyUsers)
            .ThenInclude(nu => nu.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        return rule == null ? null : MapToDto(rule);
    }

    public async Task<EscalationRuleDto> CreateAsync(CreateEscalationRuleDto dto)
    {
        var rule = new EscalationRule
        {
            Name = dto.Name,
            Description = dto.Description,
            TriggerAfterMinutes = dto.TriggerAfterMinutes,
            RepeatIntervalHours = dto.RepeatIntervalHours,
            MaxEscalations = dto.MaxEscalations,
            MessageTemplate = dto.MessageTemplate,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add trigger status relationships
        if (dto.TriggerStatusIds.Any())
        {
            var statuses = await unitOfWork.Statuses.Query()
                .Where(s => dto.TriggerStatusIds.Contains(s.Id))
                .ToListAsync();

            foreach (var status in statuses)
            {
                rule.TriggerStatuses.Add(new EscalationRuleTriggerStatus
                {
                    EscalationRule = rule,
                    Status = status,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        // Add role notification relationships
        if (dto.NotifyRoleIds.Any())
        {
            var roles = await unitOfWork.Roles.Query()
                .Where(r => dto.NotifyRoleIds.Contains(r.Id))
                .ToListAsync();

            foreach (var role in roles)
            {
                rule.NotifyRoles.Add(new EscalationRuleNotifyRole
                {
                    EscalationRule = rule,
                    Role = role,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        // Add user notification relationships
        if (dto.NotifyUserIds.Any())
        {
            var users = await unitOfWork.Users.Query()
                .Where(u => dto.NotifyUserIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in users)
            {
                rule.NotifyUsers.Add(new EscalationRuleNotifyUser
                {
                    EscalationRule = rule,
                    User = user,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        unitOfWork.EscalationRules.Add(rule);
        await unitOfWork.SaveChangesAsync();

        // Reload with relationships
        return await GetByIdAsync(rule.Id) ?? throw new InvalidOperationException("Failed to retrieve created rule");
    }

    public async Task<EscalationRuleDto?> UpdateAsync(int id, UpdateEscalationRuleDto dto)
    {
        var rule = await unitOfWork.EscalationRules.Query()
            .Include(r => r.TriggerStatuses)
            .Include(r => r.NotifyRoles)
            .Include(r => r.NotifyUsers)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name)) 
            rule.Name = dto.Name;
        
        if (dto.Description != null) 
            rule.Description = dto.Description;
        
        if (dto.TriggerAfterMinutes.HasValue) 
            rule.TriggerAfterMinutes = dto.TriggerAfterMinutes.Value;
        
        if (dto.RepeatIntervalHours.HasValue) 
            rule.RepeatIntervalHours = dto.RepeatIntervalHours.Value;
        
        if (dto.MaxEscalations.HasValue) 
            rule.MaxEscalations = dto.MaxEscalations.Value;
        
        if (dto.MessageTemplate != null) 
            rule.MessageTemplate = dto.MessageTemplate;
        
        if (dto.IsActive.HasValue) 
            rule.IsActive = dto.IsActive.Value;

        // Update trigger statuses if provided
        if (dto.TriggerStatusIds != null)
        {
            // Remove old associations
            unitOfWork.EscalationRuleTriggerStatuses.RemoveRange(rule.TriggerStatuses);
            
            // Add new associations
            var statuses = await unitOfWork.Statuses.Query()
                .Where(s => dto.TriggerStatusIds.Contains(s.Id))
                .ToListAsync();

            foreach (var status in statuses)
            {
                rule.TriggerStatuses.Add(new EscalationRuleTriggerStatus
                {
                    EscalationRule = rule,
                    Status = status,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        // Update notify roles if provided
        if (dto.NotifyRoleIds != null)
        {
            // Remove old associations
            unitOfWork.EscalationRuleNotifyRoles.RemoveRange(rule.NotifyRoles);
            
            // Add new associations
            var roles = await unitOfWork.Roles.Query()
                .Where(r => dto.NotifyRoleIds.Contains(r.Id))
                .ToListAsync();

            foreach (var role in roles)
            {
                rule.NotifyRoles.Add(new EscalationRuleNotifyRole
                {
                    EscalationRule = rule,
                    Role = role,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        // Update notify users if provided
        if (dto.NotifyUserIds != null)
        {
            // Remove old associations
            unitOfWork.EscalationRuleNotifyUsers.RemoveRange(rule.NotifyUsers);
            
            // Add new associations
            var users = await unitOfWork.Users.Query()
                .Where(u => dto.NotifyUserIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in users)
            {
                rule.NotifyUsers.Add(new EscalationRuleNotifyUser
                {
                    EscalationRule = rule,
                    User = user,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        rule.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rule = await unitOfWork.EscalationRules.GetByIdAsync(id);
        if (rule == null) return false;

        unitOfWork.EscalationRules.Remove(rule);
        await unitOfWork.SaveChangesAsync();

        return true;
    }

    private EscalationRuleDto MapToDto(EscalationRule rule)
    {
        return new EscalationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            TriggerStatusIds = rule.TriggerStatuses.Select(ts => ts.StatusId).ToList(),
            TriggerStatuses = rule.TriggerStatuses.Select(ts => new StatusDto
            {
                Id = ts.Status.Id,
                Code = ts.Status.Code,
                DisplayName = ts.Status.DisplayName,
                EntityType = ts.Status.EntityType
            }).ToList(),
            TriggerAfterMinutes = rule.TriggerAfterMinutes,
            RepeatIntervalHours = rule.RepeatIntervalHours,
            MaxEscalations = rule.MaxEscalations,
            NotifyRoleIds = rule.NotifyRoles.Select(nr => nr.RoleId).ToList(),
            NotifyRoles = rule.NotifyRoles.Select(nr => new RoleDto
            {
                Id = nr.Role.Id,
                Name = nr.Role.Name,
                Description = nr.Role.Description,
                Color = nr.Role.Color,
                IsSystemRole = nr.Role.IsSystemRole
            }).ToList(),
            NotifyUserIds = rule.NotifyUsers.Select(nu => nu.UserId).ToList(),
            NotifyUsers = rule.NotifyUsers.Select(nu => new UserDto
            {
                Id = nu.User.Id,
                FirstName = nu.User.FirstName,
                LastName = nu.User.LastName,
                Email = nu.User.Email
            }).ToList(),
            MessageTemplate = rule.MessageTemplate,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}
