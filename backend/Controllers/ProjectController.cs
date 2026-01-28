using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services.Interfaces;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/projects")]
public class ProjectsController : ControllerBase
    {
        private readonly IProjectService projectService;
        public ProjectsController(IProjectService projectService)
        {
            this.projectService = projectService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            try
            {
                var projects = await projectService.GetAllProjectsAsync();
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
                var project = await projectService.GetProjectByIdAsync(id);

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

                var project = await projectService.CreateProjectAsync(createProjectDto);

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

                var project = await projectService.UpdateProjectAsync(id, updateProjectDto);

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
                var success = await projectService.DeleteProjectAsync(id);

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