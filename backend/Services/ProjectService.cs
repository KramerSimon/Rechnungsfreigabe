public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllCostCentersAsync();
    Task<ProjectDto?> GetProjectByIdAsync(string id);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto);
    Task<ProjectDto?> UpdateProjectAsync(string id, CreateProjectDto updateProjectDto);
    Task<bool> DeleteProjectAsync(string id);
}

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.CostCenter)
            .Where(p => p.IsActive)
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
                Id = createProjectDto.Id,
                Name = createProjectDto.Name,
                Description = createProjectDto.Description,
                Budget = createProjectDto.Budget,
                CostCenterId = createProjectDto.CostCenterId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new project with ID {ProjectId}", project.Id);

            return MapToDto(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            throw;
        }
    }

    public async Task<ProjectDto?> UpdateProjectAsync(string id, CreateProjectDto updateProjectDto)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            _logger.LogWarning("Project with ID {ProjectId} not found for update", id);
            return null;
        }

        project.Name = updateProjectDto.Name;
        project.Description = updateProjectDto.Description;
        project.Budget = updateProjectDto.Budget;
        project.CostCenterId = updateProjectDto.CostCenterId;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated project with ID {ProjectId}", project.Id);

        return MapToDto(project);
    }

    private async Task<bool> DeleteProjectAsync(string id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            _logger.LogWarning("Project with ID {ProjectId} not found for deletion", id);
            return false;
        }

        project.IsActive = false;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft deleted project with ID {ProjectId}", project.Id);

        return true;
    }
}