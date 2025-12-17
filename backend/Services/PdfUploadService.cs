using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using System.Globalization;

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

            // Lies die PDF-Datei in den Speicher
            byte[] pdfContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                pdfContent = memoryStream.ToArray();
            }

            // Generiere eindeutigen Dateinamen
            var fileName = GenerateUniqueFileName(file.FileName);

            // Extrahiere Daten aus dem PDF
            var tempPath = Path.Combine(_uploadDirectory, fileName);
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await stream.WriteAsync(pdfContent, 0, pdfContent.Length);
            }

            var pdfData = ExtractInvoiceDataFromPdf(tempPath);
            
            // Verwende extrahierte Daten oder Fallback-Werte
            var invoiceNumber = pdfData.InvoiceNumber ?? ExtractInvoiceNumber(file.FileName) ?? $"INV-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            // Erstelle Invoice-Eintrag in der Datenbank mit extrahierten Daten
            var createInvoiceDto = new CreateInvoiceDto
            {
                InvoiceNumber = invoiceNumber,
                SupplierId = supplierId,
                PurchaseOrderId = purchaseOrderId,
                CostCenterId = costCenterId,
                NetAmount = pdfData.NetAmount ?? 0,
                TaxAmount = pdfData.TaxAmount ?? 0,
                TotalAmount = pdfData.TotalAmount ?? 0,
                Currency = pdfData.Currency ?? "EUR",
                InvoiceDate = pdfData.InvoiceDate ?? DateTime.UtcNow,
                DueDate = pdfData.DueDate ?? DateTime.UtcNow.AddDays(30),
                RequiresApproval = true,
                Description = pdfData.Description ?? $"PDF-Upload: {file.FileName}"
            };

            var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto, userId);

            // Speichere PDF-Content direkt in der Datenbank
            var invoiceDb = await _context.Invoices.FindAsync(invoice.Id);
            if (invoiceDb != null)
            {
                invoiceDb.PdfContent = pdfContent;
                invoiceDb.PdfFileSize = file.Length;
                invoiceDb.OriginalFilename = file.FileName;
                invoiceDb.PdfFilePath = fileName; // Speichere nur den Dateinamen für Referenzen
                await _context.SaveChangesAsync();
            }

            // Lösche die temporäre Datei
            try
            {
                System.IO.File.Delete(tempPath);
            }
            catch { }

            _logger.LogInformation($"Invoice PDF uploaded successfully to database: {fileName} for invoice {invoice.Id}");

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

    private ExtractedInvoiceData ExtractInvoiceDataFromPdf(string filePath)
    {
        try
        {
            var extractedData = new ExtractedInvoiceData();
            
            using (var pdfReader = new PdfReader(filePath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                var strategy = new LocationTextExtractionStrategy();
                var text = string.Empty;
                
                // Extrahiere Text von allen Seiten
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    text += PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy);
                }
                
                _logger.LogInformation($"Extracted text from PDF: {text.Substring(0, Math.Min(500, text.Length))}...");
                
                // Extrahiere Rechnungsnummer
                extractedData.InvoiceNumber = ExtractInvoiceNumberFromText(text);
                
                // Extrahiere Beträge
                var amounts = ExtractAmounts(text);
                extractedData.NetAmount = amounts.Net;
                extractedData.TaxAmount = amounts.Tax;
                extractedData.TotalAmount = amounts.Total;
                
                // Extrahiere Datum
                extractedData.InvoiceDate = ExtractInvoiceDate(text);
                extractedData.DueDate = ExtractDueDate(text);
                
                // Extrahiere Währung
                extractedData.Currency = ExtractCurrency(text);
                
                // Erstelle Beschreibung aus ersten Zeilen
                var lines = text.Split('\n').Take(5).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
                extractedData.Description = string.Join(" | ", lines);
                if (extractedData.Description.Length > 500)
                {
                    extractedData.Description = extractedData.Description.Substring(0, 497) + "...";
                }
            }
            
            return extractedData;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting data from PDF, using default values");
            return new ExtractedInvoiceData();
        }
    }

    private string? ExtractInvoiceNumberFromText(string text)
    {
        // Suche nach verschiedenen Rechnungsnummer-Mustern
        var patterns = new[]
        {
            @"(?:Rechnung(?:snummer)?|Invoice(?:\s+Number)?|R(?:ech)?\.?\s*Nr\.?)[:\s]+([A-Z0-9\-\/]+)",
            @"(?:Fattura|Factura)\s+N[°º.\s]*([A-Z0-9\-\/]+)",
            @"\b(?:INV|RG|RE|FAT)[-\s]*(\d{4,})",
            @"#\s*([A-Z0-9\-\/]{6,})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success && match.Groups.Count > 1)
            {
                var invoiceNumber = match.Groups[1].Value.Trim();
                if (invoiceNumber.Length >= 3 && invoiceNumber.Length <= 50)
                {
                    return invoiceNumber;
                }
            }
        }

        return null;
    }

    private (decimal? Net, decimal? Tax, decimal? Total) ExtractAmounts(string text)
    {
        decimal? netAmount = null;
        decimal? taxAmount = null;
        decimal? totalAmount = null;

        // Deutsche und italienische Dezimaltrennzeichen berücksichtigen
        var amountPattern = @"(\d{1,3}(?:[.,]\d{3})*[.,]\d{2})";
        
        // Suche nach Gesamtbetrag
        var totalPatterns = new[]
        {
            @"(?:Gesamt|Total|Totale|Importo\s+totale|Betrag)[:\s]+€?\s*" + amountPattern,
            @"(?:Summe|Sum|Somma)[:\s]+€?\s*" + amountPattern,
            @"(?:Endbetrag|Rechnungsbetrag)[:\s]+€?\s*" + amountPattern
        };

        foreach (var pattern in totalPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                totalAmount = ParseAmount(match.Groups[1].Value);
                if (totalAmount.HasValue)
                    break;
            }
        }

        // Suche nach Nettobetrag
        var netPatterns = new[]
        {
            @"(?:Netto|Net|Imponibile|Subtotal)[:\s]+€?\s*" + amountPattern,
            @"(?:Zwischensumme)[:\s]+€?\s*" + amountPattern
        };

        foreach (var pattern in netPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                netAmount = ParseAmount(match.Groups[1].Value);
                if (netAmount.HasValue)
                    break;
            }
        }

        // Suche nach MwSt/USt
        var taxPatterns = new[]
        {
            @"(?:MwSt|USt|VAT|IVA|Steuer)[:\s]+€?\s*" + amountPattern,
            @"(?:\d{1,2}%\s*MwSt)[:\s]+€?\s*" + amountPattern
        };

        foreach (var pattern in taxPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                taxAmount = ParseAmount(match.Groups[1].Value);
                if (taxAmount.HasValue)
                    break;
            }
        }

        // Wenn Gesamtbetrag gefunden wurde, aber nicht Netto und Steuer, versuche zu berechnen
        if (totalAmount.HasValue && !netAmount.HasValue && !taxAmount.HasValue)
        {
            // Annahme: 19% MwSt
            netAmount = totalAmount.Value / 1.19m;
            taxAmount = totalAmount.Value - netAmount.Value;
        }

        return (netAmount, taxAmount, totalAmount);
    }

    private decimal? ParseAmount(string amountStr)
    {
        try
        {
            // Entferne Währungssymbole
            amountStr = Regex.Replace(amountStr, @"[€$£]", "").Trim();
            
            // Bestimme, ob Punkt oder Komma als Dezimaltrennzeichen verwendet wird
            var lastComma = amountStr.LastIndexOf(',');
            var lastDot = amountStr.LastIndexOf('.');
            
            if (lastComma > lastDot)
            {
                // Deutsches Format: 1.234,56
                amountStr = amountStr.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // Englisches Format: 1,234.56
                amountStr = amountStr.Replace(",", "");
            }
            
            if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error parsing amount: {amountStr}");
        }
        
        return null;
    }

    private DateTime? ExtractInvoiceDate(string text)
    {
        var datePatterns = new[]
        {
            @"(?:Rechnungsdatum|Invoice\s+Date|Data\s+Fattura)[:\s]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})",
            @"(?:Datum|Date)[:\s]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})",
            @"(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{4})"
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                if (TryParseDate(match.Groups[1].Value, out DateTime date))
                {
                    return date;
                }
            }
        }

        return null;
    }

    private DateTime? ExtractDueDate(string text)
    {
        var datePatterns = new[]
        {
            @"(?:Fälligkeitsdatum|Due\s+Date|Scadenza)[:\s]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})",
            @"(?:Zahlbar\s+bis|Payment\s+due)[:\s]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})"
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                if (TryParseDate(match.Groups[1].Value, out DateTime date))
                {
                    return date;
                }
            }
        }

        return null;
    }

    private bool TryParseDate(string dateStr, out DateTime date)
    {
        var formats = new[]
        {
            "dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy",
            "d.M.yyyy", "d/M/yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
            "dd.MM.yy", "dd/MM/yy", "dd-MM-yy"
        };

        return DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private string ExtractCurrency(string text)
    {
        if (text.Contains("€") || Regex.IsMatch(text, @"\bEUR\b", RegexOptions.IgnoreCase))
            return "EUR";
        if (text.Contains("$") || Regex.IsMatch(text, @"\bUSD\b", RegexOptions.IgnoreCase))
            return "USD";
        if (text.Contains("£") || Regex.IsMatch(text, @"\bGBP\b", RegexOptions.IgnoreCase))
            return "GBP";
        if (text.Contains("CHF") || Regex.IsMatch(text, @"\bCHF\b", RegexOptions.IgnoreCase))
            return "CHF";
            
        return "EUR"; // Default
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

public class ExtractedInvoiceData
{
    public string? InvoiceNumber { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
}
