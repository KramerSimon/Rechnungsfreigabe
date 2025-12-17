using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.Services;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using RechnungsfreigabeAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporarily disabled for testing
public class PdfUploadController : ControllerBase
{
    private readonly IPdfUploadService _pdfUploadService;
    private readonly ILogger<PdfUploadController> _logger;
    private readonly ApplicationDbContext _context;

    public PdfUploadController(
        IPdfUploadService pdfUploadService,
        ILogger<PdfUploadController> logger,
        ApplicationDbContext context)
    {
        _pdfUploadService = pdfUploadService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Upload an invoice PDF file
    /// </summary>
    /// <param name="file">The PDF file to upload</param>
    /// <param name="supplierId">The supplier ID associated with this invoice</param>
    /// <param name="purchaseOrderId">Optional: Purchase order ID</param>
    /// <param name="costCenterId">Optional: Cost center ID</param>
    /// <returns>The created invoice DTO</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<InvoiceDto>> UploadInvoicePdf(
        [FromForm] IFormFile file,
        [FromForm] int supplierId,
        [FromForm] string? purchaseOrderId = null,
        [FromForm] string? costCenterId = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var userId = GetCurrentUserId();
            var invoice = await _pdfUploadService.UploadInvoicePdfAsync(
                file,
                supplierId,
                purchaseOrderId,
                costCenterId,
                userId);

            return CreatedAtAction("GetInvoice", "Invoices", new { id = invoice.Id }, invoice);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error during PDF upload");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operation error during PDF upload");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF");
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Download a PDF by invoice ID (from database)
    /// </summary>
    [HttpGet("download/{invoiceId}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadInvoicePdf(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Wenn PDF in Datenbank gespeichert ist, von dort servieren
            if (invoice.PdfContent != null && invoice.PdfContent.Length > 0)
            {
                var fileName = invoice.OriginalFilename ?? $"invoice_{invoiceId}.pdf";
                _logger.LogInformation($"Serving PDF from database for invoice {invoiceId}, size: {invoice.PdfContent.Length} bytes");
                return File(invoice.PdfContent, "application/pdf", fileName);
            }

            // Fallback: Versuche PDF vom Dateisystem zu laden (für alte Rechnungen)
            if (!string.IsNullOrEmpty(invoice.PdfFilePath))
            {
                var filePath = invoice.PdfFilePath;
                
                // Prüfe ob absoluter Pfad oder nur Dateiname
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "invoices", filePath);
                }

                if (System.IO.File.Exists(filePath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    var fileName = invoice.OriginalFilename ?? Path.GetFileName(filePath);
                    _logger.LogInformation($"Serving PDF from filesystem for invoice {invoiceId} (legacy): {filePath}");
                    return File(fileBytes, "application/pdf", fileName);
                }
            }

            return NotFound(new { message = "PDF not found in database or filesystem" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading PDF for invoice {invoiceId}");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Download a PDF by file path (legacy support - reads from database)
    /// </summary>
    [HttpGet("download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadByPath([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
                return BadRequest(new { message = "Path is required" });

            // Versuche PDF von Datenbank basierend auf Dateiname zu finden
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.OriginalFilename == path || i.PdfFilePath == path);

            if (invoice?.PdfContent == null)
            {
                return NotFound(new { message = "PDF not found" });
            }

            var fileName = invoice.OriginalFilename ?? "invoice.pdf";
            _logger.LogInformation($"Serving PDF from database, size: {invoice.PdfContent.Length} bytes");
            return File(invoice.PdfContent, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF by path");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an invoice PDF
    /// </summary>
    [HttpDelete("delete/{invoiceId}")]
    public async Task<IActionResult> DeleteInvoicePdf(int invoiceId)
    {
        try
        {
            var success = await _pdfUploadService.DeleteInvoicePdfAsync(invoiceId);
            
            if (!success)
            {
                return NotFound(new { message = $"Invoice {invoiceId} not found" });
            }

            return Ok(new { message = "PDF deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting PDF for invoice {invoiceId}");
            return StatusCode(500, new { message = "An error occurred while deleting the PDF" });
        }
    }

    /// <summary>
    /// Get upload statistics and status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<PdfUploadStatusDto>> GetUploadStatus()
    {
        try
        {
            var status = await _pdfUploadService.GetUploadStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload status");
            return StatusCode(500, new { message = "An error occurred while retrieving upload status" });
        }
    }

    /// <summary>
    /// Bulk upload multiple invoice PDFs
    /// </summary>
    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BulkUploadResultDto>> BulkUploadInvoicePdfs(
        [FromForm] List<IFormFile> files,
        [FromForm] int supplierId,
        [FromForm] string? purchaseOrderId = null,
        [FromForm] string? costCenterId = null)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files provided" });
            }

            var userId = GetCurrentUserId();
            var result = new BulkUploadResultDto();

            foreach (var file in files)
            {
                try
                {
                    var invoice = await _pdfUploadService.UploadInvoicePdfAsync(
                        file,
                        supplierId,
                        purchaseOrderId,
                        costCenterId,
                        userId);

                    result.SuccessfulUploads.Add(new UploadedFileDto
                    {
                        FileName = file.FileName,
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Size = file.Length
                    });
                }
                catch (Exception ex)
                {
                    result.FailedUploads.Add(new FailedFileDto
                    {
                        FileName = file.FileName,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk PDF upload");
            return StatusCode(500, new { message = "An error occurred during bulk upload" });
        }
    }

    private int GetCurrentUserId()
    {
        // Extrahiere User ID aus dem JWT Token
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
        
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) && userId > 0)
        {
            return userId;
        }
        
        // Fallback to user ID 1 (default admin) when auth is disabled for testing
        _logger.LogWarning("No valid user ID found in token, using default user ID 1");
        return 1;
    }
}

// DTOs for Bulk Upload
public class BulkUploadResultDto
{
    public List<UploadedFileDto> SuccessfulUploads { get; set; } = new();
    public List<FailedFileDto> FailedUploads { get; set; } = new();
    
    public int TotalAttempts => SuccessfulUploads.Count + FailedUploads.Count;
    public int SuccessCount => SuccessfulUploads.Count;
    public int FailureCount => FailedUploads.Count;
}

public class UploadedFileDto
{
    public string FileName { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class FailedFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
