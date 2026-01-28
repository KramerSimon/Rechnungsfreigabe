using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;


namespace RechnungsfreigabeAPI.Services.Interfaces;

public interface IEscalationRuleService
{
    Task<IEnumerable<EscalationRuleDto>> GetAllAsync();
    Task<EscalationRuleDto?> GetByIdAsync(int id);
    Task<EscalationRuleDto> CreateAsync(CreateEscalationRuleDto dto);
    Task<EscalationRuleDto?> UpdateAsync(int id, UpdateEscalationRuleDto dto);
    Task<bool> DeleteAsync(int id);
}
