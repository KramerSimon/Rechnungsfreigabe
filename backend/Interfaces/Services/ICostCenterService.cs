using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;


namespace RechnungsfreigabeAPI.Interfaces.Services;

public interface ICostCenterService
{
    Task<IEnumerable<CostCenterDto>> GetAllCostCentersAsync();
    Task<CostCenterDto?> GetCostCenterByIdAsync(string id);
    Task<CostCenterDto> CreateCostCenterAsync(CreateCostCenterDto createCostCenterDto);
    Task<CostCenterDto?> UpdateCostCenterAsync(string id, CreateCostCenterDto updateCostCenterDto);
    Task<bool> DeleteCostCenterAsync(string id);
    Task<IEnumerable<ProjectDto>> GetCostCenterProjectsAsync(string costCenterId);
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto);
}
