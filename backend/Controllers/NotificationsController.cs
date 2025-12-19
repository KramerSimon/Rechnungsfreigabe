using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;
using System.Security.Claims;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
        }

    /// <summary>
    /// Get notifications for current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return Ok(notifications);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
        }
    }

    /// <summary>
    /// Get unread notifications count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving unread count" });
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _notificationService.MarkNotificationAsReadAsync(id, userId);
            
            if (!success)
            {
                return NotFound(new { message = $"Notification with ID {id} not found" });
            }

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while marking notification as read" });
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _notificationService.MarkAllAsReadAsync(userId);
            
            if (!success)
            {
                return BadRequest(new { message = "Failed to mark all notifications as read" });
            }

            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while marking all notifications as read" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}