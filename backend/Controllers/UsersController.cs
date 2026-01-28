using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/users")]
// [Authorize] // Temporarily disabled for testing
public class UsersController : ControllerBase
{
    private readonly IUserService userService;
    public UsersController(IUserService userService)
    {
        this.userService = userService;
        }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        try
        {
            var users = await userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Get users with pagination
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsersPaged([FromQuery] PageRequest pageRequest)
    {
        try
        {
            var result = await userService.GetUsersPagedAsync(pageRequest);
            return Ok(result);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await userService.GetUserByIdAsync(id);
            
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            return Ok(user);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await userService.CreateUserAsync(createUserDto);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await userService.UpdateUserAsync(id, updateUserDto);
            
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            return Ok(user);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while updating the user" });
        }
    }

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var success = await userService.DeleteUserAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while deleting the user" });
        }
    }
}