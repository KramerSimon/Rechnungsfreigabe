using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class CostCenterService : ICostCenterService
{
    private readonly IUnitOfWork unitOfWork;
    public CostCenterService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
        }

    public async Task<IEnumerable<CostCenterDto>> GetAllCostCentersAsync()
    {
        var allCostCenters = await unitOfWork.CostCenters.GetAllWithManagerAsync();
        var costCenters = allCostCenters
            .Where(cc => cc.IsActive)
            .OrderBy(cc => cc.Name);

        return costCenters.Select(MapToDto);
    }

    public async Task<CostCenterDto?> GetCostCenterByIdAsync(string id)
    {
        var costCenter = await unitOfWork.CostCenters.GetByIdWithManagerAsync(id);
        return costCenter != null ? MapToDto(costCenter) : null;
    }

    public async Task<CostCenterDto> CreateCostCenterAsync(CreateCostCenterDto createCostCenterDto)
    {
        try
        {
            var costCenter = new CostCenter
            {
                Id = createCostCenterDto.Id,
                Name = createCostCenterDto.Name,
                Description = createCostCenterDto.Description,
                Budget = createCostCenterDto.Budget,
                ManagerId = createCostCenterDto.ManagerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            unitOfWork.CostCenters.Add(costCenter);
            await unitOfWork.SaveChangesAsync();

            // Reload with manager details
            var createdCostCenter = await unitOfWork.CostCenters.GetByIdWithManagerAsync(costCenter.Id);

            return MapToDto(createdCostCenter!);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<CostCenterDto?> UpdateCostCenterAsync(string id, CreateCostCenterDto updateCostCenterDto)
    {
        try
        {
            var costCenter = await unitOfWork.CostCenters.GetByIdWithManagerAsync(id);

            if (costCenter == null) return null;

            costCenter.Name = updateCostCenterDto.Name;
            costCenter.Description = updateCostCenterDto.Description;
            costCenter.Budget = updateCostCenterDto.Budget;
            costCenter.ManagerId = updateCostCenterDto.ManagerId;

            await unitOfWork.SaveChangesAsync();

            // Reload with updated manager details
            var updatedCostCenter = await unitOfWork.CostCenters.GetByIdWithManagerAsync(id);

            return MapToDto(updatedCostCenter!);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<bool> DeleteCostCenterAsync(string id)
    {
        try
        {
            var costCenter = await unitOfWork.CostCenters.FirstOrDefaultAsync(cc => cc.Id == id);
            if (costCenter == null) return false;

            // Check if cost center has associated invoices or projects
            var hasInvoices = await unitOfWork.CostCenters.HasRelatedInvoicesAsync(id);
            var hasProjects = await unitOfWork.CostCenters.HasRelatedProjectsAsync(id);

            if (hasInvoices || hasProjects)
            {
                // Soft delete
                costCenter.IsActive = false;
            }
            else
            {
                // Hard delete if no dependencies
                unitOfWork.CostCenters.Remove(costCenter);
            }

            await unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<IEnumerable<ProjectDto>> GetCostCenterProjectsAsync(string costCenterId)
    {
        var projects = await unitOfWork.Projects.GetByCostCenterAsync(costCenterId);
        return projects.OrderBy(p => p.Name).Select(MapProjectToDto);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
    {
        var projects = await unitOfWork.Projects.GetAllWithManagerAsync();
        return projects.OrderBy(p => p.Name).Select(MapProjectToDto);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto)
    {
        try
        {
            var projectStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                createProjectDto.Status, 
                EntityTypes.Project);
            var projectStatusId = projectStatus?.Id;
            
            var project = new Project
            {
                Id = createProjectDto.Id,
                Name = createProjectDto.Name,
                Description = createProjectDto.Description,
                CostCenterId = createProjectDto.CostCenterId,
                Budget = createProjectDto.Budget,
                StatusId = projectStatusId,
                StartDate = createProjectDto.StartDate,
                EndDate = createProjectDto.EndDate,
                ProjectManagerId = createProjectDto.ProjectManagerId,
                CreatedAt = DateTime.UtcNow
            };

            unitOfWork.Projects.Add(project);
            await unitOfWork.SaveChangesAsync();

            // Reload with includes
            var createdProject = await unitOfWork.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);

            return MapProjectToDto(createdProject!);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    private static CostCenterDto MapToDto(CostCenter costCenter)
    {
        return new CostCenterDto
        {
            Id = costCenter.Id,
            Name = costCenter.Name,
            Description = costCenter.Description,
            Budget = costCenter.Budget,
            IsActive = costCenter.IsActive,
            Manager = costCenter.Manager != null ? new UserDto
            {
                Id = costCenter.Manager.Id,
                Username = costCenter.Manager.Username,
                FirstName = costCenter.Manager.FirstName,
                LastName = costCenter.Manager.LastName,
                Email = costCenter.Manager.Email,
                IsActive = costCenter.Manager.IsActive
            } : null,
            CreatedAt = costCenter.CreatedAt
        };
    }

    private static ProjectDto MapProjectToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CostCenterId = project.CostCenterId,
            CostCenterName = project.CostCenter?.Name ?? string.Empty,
            Budget = project.Budget,
            SpentAmount = project.SpentAmount,
            Status = project.Status?.Code ?? RechnungsfreigabeAPI.Models.StatusCodes.Project.Geplant,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ProjectManagerId = project.ProjectManagerId,
            ProjectManager = project.ProjectManager != null ? new UserDto
            {
                Id = project.ProjectManager.Id,
                Username = project.ProjectManager.Username,
                FirstName = project.ProjectManager.FirstName,
                LastName = project.ProjectManager.LastName,
                Email = project.ProjectManager.Email,
                IsActive = project.ProjectManager.IsActive
            } : null,
            CreatedAt = project.CreatedAt
        };
    }
}