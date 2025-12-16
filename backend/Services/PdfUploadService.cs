using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace RechnungsfreigabeAPI.Services;

public interface IPdfUploadService
{
    Task<InvoiceDto> UploadInvoicePdfAsync(IFormFile file, int supplierId, string? purchaseOrderId, string? costCenterId, int userId);
    Task<byte[]> GetInvoicePdfAsync(int invoiceId);
    Task<bool> DeleteInvoicePdfAsync(int invoiceId);
    Task<PdfUploadStatusDto> GetUploadStatusAsync();
}

public class PdfUploadService : IPdfUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PdfUploadService> _logger;
    private readonly IInvoiceService _invoiceService;
    private readonly string _uploadDirectory;
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50 MB
    private readonly string[] _allowedExtensions = { ".pdf" };

    public PdfUploadService(
        ApplicationDbContext context,
        ILogger<PdfUploadService> logger,
        IInvoiceService invoiceService,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _logger = logger;
        _invoiceService = invoiceService;
        _uploadDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "uploads", "invoices");
        
        // Stelle sicher, dass das Upload-Verzeichnis existiert
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<InvoiceDto> UploadInvoicePdfAsync(IFormFile file, int supplierId, string? purchaseOrderId, string? costCenterId, int userId)
    {
        try
        {
            // Validiere die Datei
            ValidateFile(file);

            // Prüfe, ob Supplier existiert
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
            {
                throw new InvalidOperationException($"Supplier with ID {supplierId} not found");
            }

            // Generiere eindeutigen Dateinamen
            var fileName = GenerateUniqueFileName(file.FileName);
            var filePath = Path.Combine(_uploadDirectory, fileName);

            // Speichere die PDF-Datei
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Extrahiere Rechnungsnummer aus Dateiname (falls möglich)
            var invoiceNumber = ExtractInvoiceNumber(file.FileName);
            if (string.IsNullOrEmpty(invoiceNumber))
            {
                invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            }

            // Erstelle Invoice-Eintrag in der Datenbank
            var createInvoiceDto = new CreateInvoiceDto
            {
                InvoiceNumber = invoiceNumber,
                SupplierId = supplierId,
                PurchaseOrderId = purchaseOrderId,
                CostCenterId = costCenterId,
                NetAmount = 0, // Wird später manuell eingegeben
                TaxAmount = 0,
                TotalAmount = 0,
                Currency = "EUR",
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                RequiresApproval = true,
                Description = $"PDF-Upload: {file.FileName}"
            };

            var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto, userId);

            // Speichere PDF-Metadaten
            var invoiceDb = await _context.Invoices.FindAsync(invoice.Id);
            if (invoiceDb != null)
            {
                invoiceDb.PdfFilePath = filePath;
                invoiceDb.PdfFileSize = file.Length;
                invoiceDb.OriginalFilename = file.FileName;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Invoice PDF uploaded successfully: {fileName} for invoice {invoice.Id}");

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading invoice PDF");
            throw;
        }
    }

    public async Task<byte[]> GetInvoicePdfAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null || string.IsNullOrEmpty(invoice.PdfFilePath))
            {
                throw new FileNotFoundException($"PDF for invoice {invoiceId} not found");
            }

            if (!File.Exists(invoice.PdfFilePath))
            {
                throw new FileNotFoundException($"PDF file not found: {invoice.PdfFilePath}");
            }

            return await File.ReadAllBytesAsync(invoice.PdfFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving PDF for invoice {invoiceId}");
            throw;
        }
    }

    public async Task<bool> DeleteInvoicePdfAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(invoice.PdfFilePath) && File.Exists(invoice.PdfFilePath))
            {
                File.Delete(invoice.PdfFilePath);
            }

            invoice.PdfFilePath = null;
            invoice.PdfFileSize = null;
            invoice.OriginalFilename = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Invoice PDF deleted: {invoiceId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting PDF for invoice {invoiceId}");
            throw;
        }
    }

    public async Task<PdfUploadStatusDto> GetUploadStatusAsync()
    {
        try
        {
            var totalInvoices = await _context.Invoices.CountAsync();
            var invoicesWithPdf = await _context.Invoices
                .Where(i => !string.IsNullOrEmpty(i.PdfFilePath))
                .CountAsync();
            
            var totalPdfSize = await _context.Invoices
                .Where(i => i.PdfFileSize.HasValue)
                .SumAsync(i => i.PdfFileSize!.Value);

            var dirInfo = new DirectoryInfo(_uploadDirectory);
            var diskUsage = dirInfo.GetFiles("*", SearchOption.AllDirectories)
                .Sum(f => f.Length);

            return new PdfUploadStatusDto
            {
                TotalInvoices = totalInvoices,
                InvoicesWithPdf = invoicesWithPdf,
                TotalPdfSize = totalPdfSize,
                DiskUsageBytes = diskUsage,
                UploadDirectory = _uploadDirectory,
                MaxFileSize = _maxFileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload status");
            throw;
        }
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        if (file.Length > _maxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)} MB");
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException($"File type '{fileExtension}' is not allowed. Only PDF files are allowed.");
        }

        // Prüfe Magic Bytes für PDF
        using (var reader = new BinaryReader(file.OpenReadStream()))
        {
            var bytes = reader.ReadBytes(4);
            var header = System.Text.Encoding.ASCII.GetString(bytes);
            if (!header.StartsWith("%PDF"))
            {
                throw new ArgumentException("File is not a valid PDF file");
            }
        }
    }

    private string GenerateUniqueFileName(string originalFileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
        var extension = Path.GetExtension(originalFileName);
        return $"{timestamp}_{guid}{extension}";
    }

    private string ExtractInvoiceNumber(string fileName)
    {
        // Versuche, eine Rechnungsnummer aus dem Dateinamen zu extrahieren
        // Format: INV-2024-12345 oder Rechnung_2024_12345 etc.
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        // Vereinfachte Extraktion: Nimm die ersten 50 Zeichen
        if (nameWithoutExtension.Length > 50)
        {
            nameWithoutExtension = nameWithoutExtension.Substring(0, 50);
        }

        return nameWithoutExtension.Replace(" ", "_");
    }
}

// DTOs
public class PdfUploadStatusDto
{
    public int TotalInvoices { get; set; }
    public int InvoicesWithPdf { get; set; }
    public long TotalPdfSize { get; set; }
    public long DiskUsageBytes { get; set; }
    public string UploadDirectory { get; set; } = string.Empty;
    public long MaxFileSize { get; set; }
}
