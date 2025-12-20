using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using backend.DTOs;

namespace backend.Services
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
        Task<ProjectDto?> GetProjectByIdAsync(string id);
        Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto);
        Task<ProjectDto?> UpdateProjectAsync(string id, CreateProjectDto updateProjectDto);
        Task<bool> DeleteProjectAsync(string id);
    }

    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;

        public ProjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
        {
            var projects = await _context.Projects
                .Include(p => p.CostCenter)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return projects.Select(MapToDto);
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(string id)
        {
            var project = await _context.Projects
                .Include(p => p.CostCenter)
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null ? MapToDto(project) : null;
        }

        public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto)
        {
            var costCenter = await _context.CostCenters
                .FirstOrDefaultAsync(cc => cc.Id == createProjectDto.CostCenterId);

            if (costCenter == null)
            {
                throw new ArgumentException($"Kostenstelle {createProjectDto.CostCenterId} wurde nicht gefunden.");
            }

            var projectId = string.IsNullOrWhiteSpace(createProjectDto.Id)
                ? Guid.NewGuid().ToString("N")[..20].ToUpperInvariant()
                : createProjectDto.Id.Trim();

            var project = new Project
            {
                Id = projectId,
                Name = createProjectDto.Name,
                Description = createProjectDto.Description,
                CostCenterId = createProjectDto.CostCenterId,
                Budget = createProjectDto.Budget,
                SpentAmount = createProjectDto.SpentAmount,
                Status = MapStatus(createProjectDto.Status),
                StartDate = createProjectDto.StartDate,
                EndDate = createProjectDto.EndDate,
                ProjectManagerId = createProjectDto.ProjectManagerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return MapToDto(project);
        }

        public async Task<ProjectDto?> UpdateProjectAsync(string id, CreateProjectDto updateProjectDto)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return null;
            }

            if (!string.Equals(project.CostCenterId, updateProjectDto.CostCenterId, StringComparison.Ordinal))
            {
                var costCenterExists = await _context.CostCenters.AnyAsync(cc => cc.Id == updateProjectDto.CostCenterId);
                if (!costCenterExists)
                {
                    throw new ArgumentException($"Kostenstelle {updateProjectDto.CostCenterId} wurde nicht gefunden.");
                }
                project.CostCenterId = updateProjectDto.CostCenterId;
            }

            project.Name = updateProjectDto.Name;
            project.Description = updateProjectDto.Description;
            project.Budget = updateProjectDto.Budget;
            project.SpentAmount = updateProjectDto.SpentAmount;
            project.Status = MapStatus(updateProjectDto.Status);
            project.StartDate = updateProjectDto.StartDate;
            project.EndDate = updateProjectDto.EndDate;
            project.ProjectManagerId = updateProjectDto.ProjectManagerId;

            await _context.SaveChangesAsync();

            return MapToDto(project);
        }

        public async Task<bool> DeleteProjectAsync(string id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return false;
            }

            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            return true;
        }

        private static ProjectStatus MapStatus(string status)
        {
            if (Enum.TryParse<ProjectStatus>(status, true, out var parsed))
            {
                return parsed;
            }

            return ProjectStatus.Geplant;
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
                Status = project.Status.ToString(),
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                ProjectManagerId = project.ProjectManagerId
            };
        }
    }
}