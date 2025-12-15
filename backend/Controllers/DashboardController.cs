using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporarily disabled for testing
public class DashboardController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IInvoiceService invoiceService, ILogger<DashboardController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Get system status with actual auto-approval statistics
    /// </summary>
    [HttpGet("admin/status")]
    public async Task<ActionResult<SystemStatusDto>> GetSystemStatus()
    {
        try
        {
            // Berechne die echte Auto-Approval-Rate aus der Datenbank
            var autoApprovalRate = await _invoiceService.GetAutoApprovalRateAsync();
            var totalInvoicesThisMonth = await _invoiceService.GetInvoiceCountThisMonthAsync();
            var averageProcessingTime = await _invoiceService.GetAverageProcessingTimeAsync();

            var systemStatus = new SystemStatusDto
            {
                ServicesActive = true, // Könnte später durch echte Gesundheitsprüfungen ersetzt werden
                AutoApprovalRate = Math.Round(autoApprovalRate, 1),
                LastUpdate = DateTime.UtcNow,
                TotalInvoicesThisMonth = totalInvoicesThisMonth,
                AverageProcessingTime = Math.Round(averageProcessingTime, 1)
            };

            return Ok(systemStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return StatusCode(500, new { message = "An error occurred while retrieving system status" });
        }
    }

    /// <summary>
    /// Get user task summary
    /// </summary>
    [HttpGet("user/summary")]
    public ActionResult<UserTaskSummaryDto> GetUserTaskSummary()
    {
        try
        {
            // TODO: Implementiere echte Benutzeraufgaben-Logik
            var summary = new UserTaskSummaryDto
            {
                TotalTasks = 4,
                UrgentCount = 1,
                IncompleteCount = 1,
                OverdueCount = 1
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user task summary");
            return StatusCode(500, new { message = "An error occurred while retrieving user task summary" });
        }
    }

    /// <summary>
    /// Get accounting overview
    /// </summary>
    [HttpGet("accounting/overview")]
    public async Task<ActionResult<AccountingOverviewDto>> GetAccountingOverview()
    {
        try
        {
            var autoApprovalRate = await _invoiceService.GetAutoApprovalRateAsync();
            var rejectedStats = await _invoiceService.GetRejectedInvoiceStatsAsync();
            var readyForPaymentStats = await _invoiceService.GetReadyForPaymentStatsAsync();
            var openVolumeAmount = await _invoiceService.GetOpenVolumeAmountAsync();

            var overview = new AccountingOverviewDto
            {
                RejectedCount = rejectedStats.Count,
                RejectedAmount = rejectedStats.Amount,
                ReadyForPaymentCount = readyForPaymentStats.Count,
                ReadyForPaymentAmount = readyForPaymentStats.Amount,
                OpenVolumeAmount = openVolumeAmount,
                AutoApprovalRate = Math.Round(autoApprovalRate, 1)
            };

            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounting overview");
            return StatusCode(500, new { message = "An error occurred while retrieving accounting overview" });
        }
    }
}

// DTOs für Dashboard
public class SystemStatusDto
{
    public bool ServicesActive { get; set; }
    public double AutoApprovalRate { get; set; }
    public DateTime LastUpdate { get; set; }
    public int TotalInvoicesThisMonth { get; set; }
    public double AverageProcessingTime { get; set; }
}

public class UserTaskSummaryDto
{
    public int TotalTasks { get; set; }
    public int UrgentCount { get; set; }
    public int IncompleteCount { get; set; }
    public int OverdueCount { get; set; }
}

public class AccountingOverviewDto
{
    public int RejectedCount { get; set; }
    public decimal RejectedAmount { get; set; }
    public int ReadyForPaymentCount { get; set; }
    public decimal ReadyForPaymentAmount { get; set; }
    public decimal OpenVolumeAmount { get; set; }
    public double AutoApprovalRate { get; set; }
}

public class InvoiceStatsDto
{
    public int Count { get; set; }
    public decimal Amount { get; set; }
}