using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/invoices/{invoiceId}/[controller]")]
// [Authorize] // Temporarily disabled for testing
public class HistoryController : ControllerBase
{
    private readonly IInvoiceHistoryService _historyService;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(IInvoiceHistoryService historyService, ILogger<HistoryController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// Get invoice history as timeline
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <returns>Timeline of invoice history</returns>
    [HttpGet("timeline")]
    public async Task<ActionResult<List<InvoiceHistoryTimelineDto>>> GetInvoiceHistoryTimeline(int invoiceId)
    {
        try
        {
            _logger.LogInformation("Getting history timeline for invoice {InvoiceId}", invoiceId);
            
            // Temporarily return mock data until database is updated
            var mockTimeline = new List<InvoiceHistoryTimelineDto>
            {
                new InvoiceHistoryTimelineDto
                {
                    Date = "2024-12-11",
                    Entries = new List<InvoiceHistoryDto>
                    {
                        new InvoiceHistoryDto
                        {
                            Id = 1,
                            InvoiceId = invoiceId,
                            Action = "Rechnung eingereicht",
                            ActionType = "Submitted",
                            ActionSource = "User",
                            ChangedAt = DateTime.Parse("2024-12-11T09:00:00Z"),
                            ChangedByUser = new DTOs.UserDto 
                            { 
                                Id = 1, 
                                Username = "mmustermann", 
                                FirstName = "Max", 
                                LastName = "Mustermann"
                            },
                            Comments = "Rechnung wurde vom Lieferanten eingereicht",
                            DisplayIcon = "description",
                            DisplayColor = "primary"
                        },
                        new InvoiceHistoryDto
                        {
                            Id = 2,
                            InvoiceId = invoiceId,
                            Action = "Kostenstelle zugewiesen",
                            ActionType = "FieldChange",
                            ActionSource = "User",
                            ChangedAt = DateTime.Parse("2024-12-11T10:30:00Z"),
                            ChangedByUser = new DTOs.UserDto 
                            { 
                                Id = 2, 
                                Username = "aschmidt", 
                                FirstName = "Anna", 
                                LastName = "Schmidt"
                            },
                            FieldChanges = new Dictionary<string, object> 
                            { 
                                { "costCenter", new { oldValue = (string?)null, newValue = "4020 - IT" } } 
                            },
                            DisplayIcon = "edit",
                            DisplayColor = "accent"
                        },
                        new InvoiceHistoryDto
                        {
                            Id = 3,
                            InvoiceId = invoiceId,
                            Action = "Automatische Eskalation",
                            ActionType = "Escalation",
                            ActionSource = "System",
                            ChangedAt = DateTime.Parse("2024-12-11T14:00:00Z"),
                            Comments = "Rechnung automatisch an nächste Freigabeebene weitergeleitet",
                            PolicyReference = "POLICY_ESCALATION_24H",
                            SystemReason = "24h Freigabefrist überschritten",
                            DisplayIcon = "trending_up",
                            DisplayColor = "warn"
                        }
                    }
                }
            };
            
            return Ok(mockTimeline);
            
            // TODO: Uncomment this when database is updated
            // var timeline = await _historyService.GetInvoiceHistoryTimelineAsync(invoiceId);
            // return Ok(timeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice history timeline for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, "Fehler beim Laden der Rechnungshistorie");
        }
    }

    /// <summary>
    /// Get raw invoice history entries
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <returns>List of history entries</returns>
    [HttpGet]
    public async Task<ActionResult<List<InvoiceHistoryDto>>> GetInvoiceHistory(int invoiceId)
    {
        try
        {
            var history = await _historyService.GetInvoiceHistoryAsync(invoiceId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice history for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, "Fehler beim Laden der Rechnungshistorie");
        }
    }

    /// <summary>
    /// Create a manual history entry (for administrative purposes)
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="createHistoryDto">History entry details</param>
    /// <returns>Success result</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateHistoryEntry(int invoiceId, [FromBody] CreateHistoryEntryDto createHistoryDto)
    {
        try
        {
            createHistoryDto.InvoiceId = invoiceId; // Ensure consistency
            await _historyService.CreateHistoryEntryAsync(createHistoryDto);
            return Ok(new { message = "Historie-Eintrag erfolgreich erstellt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating history entry for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, "Fehler beim Erstellen des Historie-Eintrags");
        }
    }
}