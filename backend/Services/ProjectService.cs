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
        try
        {
            var project = new Project
            {
                Id = Guid.NewGuid().ToString().Substring(0, 20),
                Name = createProjectDto.Name,
                Description = createProjectDto.Description,
                CostCenterId = "CC-DEFAULT",
                Budget = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return MapToDto(project);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<ProjectDto?> UpdateProjectAsync(string id, CreateProjectDto updateProjectDto)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            
            return null;
        }

        project.Name = updateProjectDto.Name;
        project.Description = updateProjectDto.Description;

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

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id.GetHashCode(),
            Name = project.Name,
            Description = project.Description ?? string.Empty
        };
    }
}
}