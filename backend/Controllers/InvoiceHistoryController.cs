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
            
            var timeline = await _historyService.GetInvoiceHistoryTimelineAsync(invoiceId);
            return Ok(timeline);
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