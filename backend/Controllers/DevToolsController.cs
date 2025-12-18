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
    private readonly INotificationService _notificationService;
    private readonly ILogger<DevToolsController> _logger;

    public DevToolsController(
        ApplicationDbContext context, 
        IPasswordService passwordService,
        INotificationService notificationService,
        ILogger<DevToolsController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _notificationService = notificationService;
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
    /// Backfill notifications for all pending approval workflows where none exist yet.
    /// DEVELOPMENT ONLY – helps populate notifications for existing data.
    /// </summary>
    [HttpPost("backfill-notifications")]
    public async Task<IActionResult> BackfillNotifications()
    {
        try
        {
            var pendingWorkflows = await _context.ApprovalWorkflows
                .Include(w => w.Approver)
                .Include(w => w.Invoice)
                    .ThenInclude(i => i!.Supplier)
                .Where(w => w.Status == Models.ApprovalStatus.Pending)
                .ToListAsync();

            int created = 0;

            foreach (var wf in pendingWorkflows)
            {
                if (wf.Invoice == null || wf.Approver == null) continue;

                // Check if a notification already exists for this approver & invoice & type
                var hasExisting = await _context.Notifications.AnyAsync(n =>
                    n.UserId == wf.ApproverId &&
                    n.InvoiceId == wf.InvoiceId &&
                    n.Type == "invoice_approval_required");

                if (!hasExisting)
                {
                    await _notificationService.CreateNotificationAsync(
                        wf.ApproverId,
                        "invoice_approval_required",
                        "Neue Rechnung zur Freigabe",
                        $"Rechnung {wf.Invoice.InvoiceNumber} von {wf.Invoice.Supplier!.Name} über {wf.Invoice.TotalAmount:C} EUR wartet auf Ihre Freigabe.",
                        wf.InvoiceId,
                        Models.NotificationPriority.Normal
                    );
                    created++;
                }
            }

            return Ok(new { message = "Backfill abgeschlossen", created });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error backfilling notifications");
            return StatusCode(500, new { message = "Fehler beim Backfill der Benachrichtigungen" });
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