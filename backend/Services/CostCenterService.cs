using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Services;

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

public class CostCenterService : ICostCenterService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CostCenterService> _logger;

    public CostCenterService(ApplicationDbContext context, ILogger<CostCenterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CostCenterDto>> GetAllCostCentersAsync()
    {
        var costCenters = await _context.CostCenters
            .Include(cc => cc.Manager)
            .Where(cc => cc.IsActive)
            .OrderBy(cc => cc.Name)
            .ToListAsync();

        return costCenters.Select(MapToDto);
    }

    public async Task<CostCenterDto?> GetCostCenterByIdAsync(string id)
    {
        var costCenter = await _context.CostCenters
            .Include(cc => cc.Manager)
            .FirstOrDefaultAsync(cc => cc.Id == id);

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

            _context.CostCenters.Add(costCenter);
            await _context.SaveChangesAsync();

            // Reload with manager details
            var createdCostCenter = await _context.CostCenters
                .Include(cc => cc.Manager)
                .FirstAsync(cc => cc.Id == costCenter.Id);

            _logger.LogInformation("Cost center created successfully: {CostCenterId}", costCenter.Id);
            return MapToDto(createdCostCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cost center: {CostCenterId}", createCostCenterDto.Id);
            throw;
        }
    }

    public async Task<CostCenterDto?> UpdateCostCenterAsync(string id, CreateCostCenterDto updateCostCenterDto)
    {
        try
        {
            var costCenter = await _context.CostCenters
                .Include(cc => cc.Manager)
                .FirstOrDefaultAsync(cc => cc.Id == id);

            if (costCenter == null) return null;

            costCenter.Name = updateCostCenterDto.Name;
            costCenter.Description = updateCostCenterDto.Description;
            costCenter.Budget = updateCostCenterDto.Budget;
            costCenter.ManagerId = updateCostCenterDto.ManagerId;

            await _context.SaveChangesAsync();

            // Reload with updated manager details
            var updatedCostCenter = await _context.CostCenters
                .Include(cc => cc.Manager)
                .FirstAsync(cc => cc.Id == id);

            _logger.LogInformation("Cost center updated successfully: {CostCenterId}", id);
            return MapToDto(updatedCostCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cost center with ID: {CostCenterId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCostCenterAsync(string id)
    {
        try
        {
            var costCenter = await _context.CostCenters.FindAsync(id);
            if (costCenter == null) return false;

            // Check if cost center has associated invoices or projects
            var hasInvoices = await _context.Invoices.AnyAsync(i => i.CostCenterId == id);
            var hasProjects = await _context.Projects.AnyAsync(p => p.CostCenterId == id);

            if (hasInvoices || hasProjects)
            {
                // Soft delete
                costCenter.IsActive = false;
            }
            else
            {
                // Hard delete if no dependencies
                _context.CostCenters.Remove(costCenter);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cost center deleted: {CostCenterId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cost center with ID: {CostCenterId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ProjectDto>> GetCostCenterProjectsAsync(string costCenterId)
    {
        var projects = await _context.Projects
            .Include(p => p.CostCenter)
            .Include(p => p.ProjectManager)
            .Where(p => p.CostCenterId == costCenterId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return projects.Select(MapProjectToDto);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.CostCenter)
            .Include(p => p.ProjectManager)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return projects.Select(MapProjectToDto);
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
                CostCenterId = createProjectDto.CostCenterId,
                Budget = createProjectDto.Budget,
                Status = Enum.Parse<ProjectStatus>(createProjectDto.Status),
                StartDate = createProjectDto.StartDate,
                EndDate = createProjectDto.EndDate,
                ProjectManagerId = createProjectDto.ProjectManagerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Reload with includes
            var createdProject = await _context.Projects
                .Include(p => p.CostCenter)
                .Include(p => p.ProjectManager)
                .FirstOrDefaultAsync(p => p.Id == project.Id);

            return MapProjectToDto(createdProject!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project: {ProjectName}", createProjectDto.Name);
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
            CostCenterName = project.CostCenter.Name,
            Budget = project.Budget,
            SpentAmount = project.SpentAmount,
            Status = project.Status.ToString(),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ProjectManager = project.ProjectManager != null ? new UserDto
            {
                Id = project.ProjectManager.Id,
                Username = project.ProjectManager.Username,
                FirstName = project.ProjectManager.FirstName,
                LastName = project.ProjectManager.LastName
            } : null,
            CreatedAt = project.CreatedAt
        };
    }
}