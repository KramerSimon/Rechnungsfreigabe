using Microsoft.AspNetCore.Mvc;
using backend.DTOs;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            try
            {
                var projects = await _projectService.GetAllProjectsAsync();
                return Ok(projects);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving projects" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(string id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);

                if (project == null)
                {
                    return NotFound(new { message = $"Project with ID {id} not found" });
                }

                return Ok(project);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the project" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto createProjectDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var project = await _projectService.CreateProjectAsync(createProjectDto);

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while creating the project" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectDto>> UpdateProject(string id, [FromBody] CreateProjectDto updateProjectDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var project = await _projectService.UpdateProjectAsync(id, updateProjectDto);

                if (project == null)
                {
                    return NotFound(new { message = $"Proejct with ID {id} not found" });
                }

                return Ok(project);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while updating the project" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(string id)
        {
            try
            {
                var success = await _projectService.DeleteProjectAsync(id);

                if (!success)
                {
                    return NotFound(new { message = $"Project with ID {id} not found" });
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the project" });
            }
        }
    }
}