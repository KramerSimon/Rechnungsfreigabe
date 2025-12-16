using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.Services;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporarily disabled for testing
public class PdfUploadController : ControllerBase
{
    private readonly IPdfUploadService _pdfUploadService;
    private readonly ILogger<PdfUploadController> _logger;

    public PdfUploadController(
        IPdfUploadService pdfUploadService,
        ILogger<PdfUploadController> logger)
    {
        _pdfUploadService = pdfUploadService;
        _logger = logger;
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
    /// Download an invoice PDF by invoice ID
    /// </summary>
    [HttpGet("download/{invoiceId}")]
    public async Task<IActionResult> DownloadInvoicePdf(int invoiceId)
    {
        try
        {
            var pdfBytes = await _pdfUploadService.GetInvoicePdfAsync(invoiceId);
            return File(pdfBytes, "application/pdf", $"invoice_{invoiceId}.pdf");
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, $"PDF not found for invoice {invoiceId}");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading PDF for invoice {invoiceId}");
            return StatusCode(500, new { message = "An error occurred while downloading the file" });
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
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
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
