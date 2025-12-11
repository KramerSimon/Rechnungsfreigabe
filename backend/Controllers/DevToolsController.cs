using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Services;
using BC = BCrypt.Net.BCrypt;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevToolsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<DevToolsController> _logger;

    public DevToolsController(
        ApplicationDbContext context, 
        IPasswordService passwordService,
        ILogger<DevToolsController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Temporärer Endpoint zum Reparieren der Passwort-Hashes
    /// NUR FÜR ENTWICKLUNG - NICHT IN PRODUKTION VERWENDEN!
    /// </summary>
    [HttpPost("fix-passwords")]
    public async Task<IActionResult> FixPasswords()
    {
        try
        {
            var password = "password123";
            var results = new List<object>();

            // Alle Testbenutzer
            var usernames = new[] { "admin", "max.mustermann", "maria.mueller", "hans.schmidt", "lisa.klein" };

            foreach (var username in usernames)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user != null)
                {
                    var newHash = _passwordService.HashPassword(password);
                    var oldHash = user.PasswordHash;
                    
                    user.PasswordHash = newHash;
                    user.FailedLoginAttempts = 0;
                    user.LockedUntil = null;
                    user.UpdatedAt = DateTime.UtcNow;

                    results.Add(new
                    {
                        username = username,
                        oldHash = oldHash,
                        newHash = newHash,
                        success = true
                    });

                    _logger.LogInformation("Updated password hash for user: {Username}", username);
                }
                else
                {
                    results.Add(new
                    {
                        username = username,
                        error = "User not found",
                        success = false
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Passwort-Hashes erfolgreich aktualisiert",
                password = password,
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing password hashes");
            return StatusCode(500, "Fehler beim Aktualisieren der Passwort-Hashes");
        }
    }

    /// <summary>
    /// Test BCrypt hash generation
    /// </summary>
    [HttpGet("test-hash/{password}")]
    public IActionResult TestHash(string password)
    {
        var hash = _passwordService.HashPassword(password);
        var verify = _passwordService.VerifyPassword(password, hash);
        
        return Ok(new
        {
            password = password,
            hash = hash,
            verificationSuccessful = verify
        });
    }
}