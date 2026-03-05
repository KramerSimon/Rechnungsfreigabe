using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.Interfaces.Services.Implementations;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using RechnungsfreigabeAPI.Data;
using Microsoft.EntityFrameworkCore;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/pdf-upload")]
[Authorize]
public class PdfUploadController : ControllerBase
{
    private readonly IPdfUploadService pdfUploadService;
    private readonly ApplicationDbContext context;

    public PdfUploadController(
        IPdfUploadService pdfUploadService,
        ApplicationDbContext context)
    {
        this.pdfUploadService = pdfUploadService;
        this.context = context;
    }

    /// <summary>
    /// Upload an invoice PDF or XML file
    /// </summary>
    /// <param name="file">The PDF or XML file to upload</param>
    /// <param name="supplierId">Optional: The supplier ID associated with this invoice (will be extracted from PDF if not provided)</param>
    /// <param name="purchaseOrderId">Optional: Purchase order ID</param>
    /// <param name="costCenterId">Optional: Cost center ID</param>
    /// <param name="projectId">Optional: Project ID</param>
    /// <returns>The created invoice DTO</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<InvoiceDto>> UploadInvoicePdf(
        [FromForm] IFormFile file,
        [FromForm] int? supplierId = null,
        [FromForm] string? purchaseOrderId = null,
        [FromForm] string? costCenterId = null,
        [FromForm] string? projectId = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var userId = GetCurrentUserId();
            var result = await pdfUploadService.UploadInvoicePdfAsync(
                file,
                supplierId,
                purchaseOrderId,
                costCenterId,
                projectId,
                userId);

            // Check if data was extracted
            var hasExtractedData = result.TotalAmount > 0 || (result.Supplier != null && !string.IsNullOrEmpty(result.Supplier.Name));
            
            if (!hasExtractedData)
            {
                return Ok(new
                {
                    invoice = result,
                    warning = "Datei hochgeladen, aber keine Daten extrahiert. Das Dokument enth�lt m�glicherweise nur Bilder (gescanntes Dokument). Bitte Rechnungsdaten manuell vervollst�ndigen.",
                    requiresManualEntry = true
                });
            }

            return CreatedAtAction("GetInvoice", "Invoices", new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Upload a purchase order PDF or XML file
    /// </summary>
    /// <param name="file">The PDF or XML file to upload</param>
    /// <param name="supplierId">Optional: The supplier ID associated with this PO (will be extracted from PDF if not provided)</param>
    /// <param name="costCenterId">Optional: Cost center ID</param>
    /// <param name="projectId">Optional: Project ID</param>
    /// <returns>The created purchase order DTO</returns>
    [HttpPost("upload-purchase-order")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PurchaseOrderDto>> UploadPurchaseOrderPdf(
        [FromForm] IFormFile file,
        [FromForm] int? supplierId = null,
        [FromForm] string? costCenterId = null,
        [FromForm] string? projectId = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var userId = GetCurrentUserId();
            var result = await pdfUploadService.UploadPurchaseOrderPdfAsync(
                file,
                supplierId,
                costCenterId,
                projectId,
                userId);

            // Check if data was extracted
            var hasExtractedData = result.TotalAmount > 0;
            
            if (!hasExtractedData)
            {
                return Ok(new
                {
                    purchaseOrder = result,
                    warning = "Datei hochgeladen, aber keine Daten extrahiert. Das Dokument enth�lt m�glicherweise nur Bilder (gescanntes Dokument). Bitte Daten manuell vervollst�ndigen.",
                    requiresManualEntry = true
                });
            }

            return CreatedAtAction("GetPurchaseOrder", "PurchaseOrders", new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
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
            var invoice = await context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            if (invoice.PdfContent != null && invoice.PdfContent.Length > 0)
            {
                var fileName = invoice.OriginalFilename ?? $"invoice_{invoiceId}.pdf";
                return File(invoice.PdfContent, "application/pdf", fileName, enableRangeProcessing: true);
            }

            return NotFound(new { message = "PDF not found in database" });
        }
        catch (Exception ex)
        {
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
            var invoice = await context.Invoices
                .FirstOrDefaultAsync(i => i.OriginalFilename == path);

            if (invoice?.PdfContent == null)
            {
                return NotFound(new { message = "PDF not found" });
            }

            var fileName = invoice.OriginalFilename ?? "invoice.pdf";
            return File(invoice.PdfContent, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
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
            var success = await pdfUploadService.DeleteInvoicePdfAsync(invoiceId);
            
            if (!success)
            {
                return NotFound(new { message = $"Invoice {invoiceId} not found" });
            }

            return Ok(new { message = "PDF deleted successfully" });
        }
        catch (Exception)
        {
            
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
            var status = await pdfUploadService.GetUploadStatusAsync();
            return Ok(status);
        }
        catch (Exception)
        {
            
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
        [FromForm] string? costCenterId = null,
        [FromForm] string? projectId = null)
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
                    var invoice = await pdfUploadService.UploadInvoicePdfAsync(
                        file,
                        supplierId,
                        purchaseOrderId,
                        costCenterId,
                        projectId,
                        userId);

                    // Check if data was extracted
                    var hasExtractedData = invoice.TotalAmount > 0 || (invoice.Supplier != null && !string.IsNullOrEmpty(invoice.Supplier.Name));

                    var uploadedFile = new UploadedFileDto
                    {
                        FileName = file.FileName,
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Size = file.Length
                    };

                    // Add warning if no data was extracted
                    if (!hasExtractedData)
                    {
                        uploadedFile.Warning = "Keine Daten extrahiert - gescanntes Dokument?";
                        uploadedFile.RequiresManualEntry = true;
                    }

                    result.SuccessfulUploads.Add(uploadedFile);
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
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred during bulk upload" });
        }
    }

    private int GetCurrentUserId()
    {
        // Extrahiere User ID aus dem JWT Token
        var userIdClaim = User.FindFirst("sub") ?? 
                         User.FindFirst("nameid") ?? 
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) && userId > 0)
        {
            return userId;
        }
        
        // Fallback to user ID 1 (default admin) when auth is disabled for testing
        
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
    public string? Warning { get; set; }
    public bool RequiresManualEntry { get; set; }
}

public class FailedFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
