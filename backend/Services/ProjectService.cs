using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Repositories.Interfaces;
using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services.Interfaces;

namespace RechnungsfreigabeAPI.Services;

public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProjectService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
        {
            var projects = await _unitOfWork.Projects.Query()
                .Include(p => p.CostCenter)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return projects.Select(MapToDto);
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(string id)
        {
            var project = await _unitOfWork.Projects.Query()
                .Include(p => p.CostCenter)
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null ? MapToDto(project) : null;
        }

        public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto)
        {
            var costCenter = await _unitOfWork.CostCenters.Query()
                .FirstOrDefaultAsync(cc => cc.Id == createProjectDto.CostCenterId);

            if (costCenter == null)
            {
                throw new ArgumentException($"Kostenstelle {createProjectDto.CostCenterId} wurde nicht gefunden.");
            }

            var projectId = string.IsNullOrWhiteSpace(createProjectDto.Id)
                ? Guid.NewGuid().ToString("N")[..20].ToUpperInvariant()
                : createProjectDto.Id.Trim();

            var statusId = await GetProjectStatusIdAsync(createProjectDto.Status ?? RechnungsfreigabeAPI.Models.StatusCodes.Project.Geplant);

            var project = new Project
            {
                Id = projectId,
                Name = createProjectDto.Name,
                Description = createProjectDto.Description,
                CostCenterId = createProjectDto.CostCenterId,
                Budget = createProjectDto.Budget,
                SpentAmount = createProjectDto.SpentAmount,
                StatusId = statusId,
                StartDate = createProjectDto.StartDate,
                EndDate = createProjectDto.EndDate,
                ProjectManagerId = createProjectDto.ProjectManagerId,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Projects.Add(project);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(project);
        }

        public async Task<ProjectDto?> UpdateProjectAsync(string id, CreateProjectDto updateProjectDto)
        {
            var project = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return null;
            }

            if (!string.Equals(project.CostCenterId, updateProjectDto.CostCenterId, StringComparison.Ordinal))
            {
                var costCenterExists = await _unitOfWork.CostCenters.Query().AnyAsync(cc => cc.Id == updateProjectDto.CostCenterId);
                if (!costCenterExists)
                {
                    throw new ArgumentException($"Kostenstelle {updateProjectDto.CostCenterId} wurde nicht gefunden.");
                }
                project.CostCenterId = updateProjectDto.CostCenterId;
            }

            var statusId = await GetProjectStatusIdAsync(updateProjectDto.Status ?? RechnungsfreigabeAPI.Models.StatusCodes.Project.Geplant);

            project.Name = updateProjectDto.Name;
            project.Description = updateProjectDto.Description;
            project.Budget = updateProjectDto.Budget;
            project.SpentAmount = updateProjectDto.SpentAmount;
            project.StatusId = statusId;
            project.StartDate = updateProjectDto.StartDate;
            project.EndDate = updateProjectDto.EndDate;
            project.ProjectManagerId = updateProjectDto.ProjectManagerId;

            await _unitOfWork.SaveChangesAsync();

            return MapToDto(project);
        }

        public async Task<bool> DeleteProjectAsync(string id)
        {
            var project = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return false;
            }

            _unitOfWork.Projects.Remove(project);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private async Task<int?> GetProjectStatusIdAsync(string statusCode)
        {
            var status = await _unitOfWork.Statuses.Query()
                .Where(s => s.Code == statusCode && s.EntityType == EntityTypes.Project)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
            return status;
        }

        private static ProjectDto MapToDto(Project project)
        {
            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CostCenterId = project.CostCenterId,
                CostCenterName = project.CostCenter?.Name,
                Budget = project.Budget,
                SpentAmount = project.SpentAmount,
                Status = project.Status?.Code ?? RechnungsfreigabeAPI.Models.StatusCodes.Project.Geplant,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                ProjectManagerId = project.ProjectManagerId
            };
        }
}