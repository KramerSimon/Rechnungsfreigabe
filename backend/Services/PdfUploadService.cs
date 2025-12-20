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
using Tesseract;
using PDFtoImage;

namespace RechnungsfreigabeAPI.Services;

public interface IPdfUploadService
{
    Task<InvoiceDto> UploadInvoicePdfAsync(IFormFile file, int? supplierId, string? purchaseOrderId, string? costCenterId, int userId);
    Task<byte[]> GetInvoicePdfAsync(int invoiceId);
    Task<bool> DeleteInvoicePdfAsync(int invoiceId);
    Task<PdfUploadStatusDto> GetUploadStatusAsync();
}

public class PdfUploadService : IPdfUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly string _uploadDirectory;
    private readonly string _tessdataPath;
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50 MB
    private readonly string[] _allowedExtensions = { ".pdf" };

    public PdfUploadService(
        ApplicationDbContext context,
        IInvoiceService invoiceService,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _invoiceService = invoiceService;
        _uploadDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "uploads", "invoices");
        _tessdataPath = Path.Combine(webHostEnvironment.ContentRootPath, "tessdata");
        
        // Stelle sicher, dass das Upload-Verzeichnis existiert
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }

        // Stelle sicher, dass das Tessdata-Verzeichnis existiert
        if (!Directory.Exists(_tessdataPath))
        {
            Directory.CreateDirectory(_tessdataPath);
            Console.WriteLine($"[OCR] Created tessdata directory at: {_tessdataPath}");
            Console.WriteLine($"[OCR] Please download language files from: https://github.com/tesseract-ocr/tessdata");
            Console.WriteLine($"[OCR] Required: deu.traineddata (German), ita.traineddata (Italian)");
        }
    }

    public async Task<InvoiceDto> UploadInvoicePdfAsync(IFormFile file, int? supplierId, string? purchaseOrderId, string? costCenterId, int userId)
    {
        try
        {
            Console.WriteLine($"[PDF Upload] Starting upload for file: {file.FileName}, Size: {file.Length} bytes");

            // Validiere die Datei
            ValidateFile(file);
            Console.WriteLine($"[PDF Upload] File validation passed");

            // Validiere dass der User existiert
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }
            Console.WriteLine($"[PDF Upload] User {userId} validated");

            // Lies die PDF-Datei in den Speicher
            byte[] pdfContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                pdfContent = memoryStream.ToArray();
            }
            Console.WriteLine($"[PDF Upload] PDF content loaded into memory: {pdfContent.Length} bytes");

            // Generiere eindeutigen Dateinamen
            var fileName = GenerateUniqueFileName(file.FileName);
            Console.WriteLine($"[PDF Upload] Generated unique filename: {fileName}");

            // Extrahiere Daten aus dem PDF
            var tempPath = Path.Combine(_uploadDirectory, fileName);
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await stream.WriteAsync(pdfContent, 0, pdfContent.Length);
            }
            Console.WriteLine($"[PDF Upload] Temporary file created at: {tempPath}");

            var pdfData = ExtractInvoiceDataFromPdf(tempPath);
            Console.WriteLine($"[PDF Upload] Data extracted - InvoiceNum: {pdfData.InvoiceNumber}, Total: {pdfData.TotalAmount}, Supplier: {pdfData.SupplierInfo?.Name}");

            // Generiere Rechnungsnummer im Format FAT-{Nummer}-{Jahr}
            var invoiceNumber = await GenerateInvoiceNumberAsync();
            Console.WriteLine($"[PDF Upload] Generated invoice number: {invoiceNumber}");

            // Finde oder erstelle Lieferant
            int finalSupplierId;
            if (supplierId.HasValue && supplierId.Value > 0)
            {
                // Verwende die übergebene SupplierId
                var supplier = await _context.Suppliers.FindAsync(supplierId.Value);
                if (supplier == null)
                {
                    Console.WriteLine($"[PDF Upload] ERROR: Supplier with ID {supplierId} not found");
                    throw new InvalidOperationException($"Supplier with ID {supplierId} not found");
                }
                finalSupplierId = supplierId.Value;
                Console.WriteLine($"[PDF Upload] Using provided supplier ID: {finalSupplierId}");
            }
            else
            {
                // Extrahiere und erstelle/finde Lieferant aus PDF
                finalSupplierId = await FindOrCreateSupplierAsync(pdfData.SupplierInfo, tempPath);
                Console.WriteLine($"[PDF Upload] Found/created supplier ID: {finalSupplierId}");
            }

            // Validiere Purchase Order ID falls angegeben
            string? validatedPurchaseOrderId = null;
            if (!string.IsNullOrWhiteSpace(purchaseOrderId))
            {
                var purchaseOrderExists = await _context.PurchaseOrders.AnyAsync(po => po.Id == purchaseOrderId);
                if (!purchaseOrderExists)
                {
                    // Log warnung aber blockiere nicht - setze einfach null
                    Console.WriteLine($"[PDF Upload] Warning: Purchase Order '{purchaseOrderId}' not found, setting to null");
                }
                else
                {
                    validatedPurchaseOrderId = purchaseOrderId;
                    Console.WriteLine($"[PDF Upload] Validated Purchase Order: {validatedPurchaseOrderId}");
                }
            }

            // Validiere Cost Center ID falls angegeben
            string? validatedCostCenterId = null;
            if (!string.IsNullOrWhiteSpace(costCenterId))
            {
                var costCenterExists = await _context.CostCenters.AnyAsync(cc => cc.Id == costCenterId);
                if (!costCenterExists)
                {
                    // Log warnung aber blockiere nicht - setze einfach null
                    Console.WriteLine($"[PDF Upload] Warning: Cost Center '{costCenterId}' not found, setting to null");
                }
                else
                {
                    validatedCostCenterId = costCenterId;
                    Console.WriteLine($"[PDF Upload] Validated Cost Center: {validatedCostCenterId}");
                }
            }

            // Erstelle Invoice-Eintrag in der Datenbank mit extrahierten Daten
            var createInvoiceDto = new CreateInvoiceDto
            {
                InvoiceNumber = invoiceNumber,
                SupplierId = finalSupplierId,
                PurchaseOrderId = validatedPurchaseOrderId,
                CostCenterId = validatedCostCenterId,
                NetAmount = pdfData.NetAmount ?? 0,
                TaxAmount = pdfData.TaxAmount ?? 0,
                TotalAmount = pdfData.TotalAmount ?? 0,
                Currency = pdfData.Currency ?? "EUR",
                InvoiceDate = pdfData.InvoiceDate ?? DateTime.UtcNow,
                DueDate = pdfData.DueDate ?? DateTime.UtcNow.AddDays(30),
                RequiresApproval = true,
                Description = pdfData.Description ?? $"PDF-Upload: {file.FileName}"
            };

            Console.WriteLine($"[PDF Upload] Creating invoice with data - Number: {createInvoiceDto.InvoiceNumber}, Total: {createInvoiceDto.TotalAmount}, Net: {createInvoiceDto.NetAmount}, Tax: {createInvoiceDto.TaxAmount}");

            InvoiceDto invoice;
            try
            {
                invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto, userId);
                Console.WriteLine($"[PDF Upload] Invoice created successfully with ID: {invoice.Id}");
            }
            catch (Exception ex)
            {
                // Preserve context but surface the failure message with full exception details
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                var fullMessage = $"Failed to create invoice: {ex.Message}";
                if (ex.InnerException != null)
                {
                    fullMessage += $" | Inner: {innerMessage}";
                    if (ex.InnerException.InnerException != null)
                    {
                        fullMessage += $" | InnerInner: {ex.InnerException.InnerException.Message}";
                    }
                }
                Console.WriteLine($"[PDF Upload] ERROR creating invoice: {fullMessage}");
                throw new InvalidOperationException(fullMessage, ex);
            }

            // Speichere PDF-Content direkt in der Datenbank
            try
            {
                var invoiceDb = await _context.Invoices.FindAsync(invoice.Id);
                if (invoiceDb != null)
                {
                    invoiceDb.PdfContent = pdfContent;
                    invoiceDb.PdfFileSize = file.Length;
                    invoiceDb.OriginalFilename = file.FileName;
                    invoiceDb.PdfFilePath = fileName; // Speichere nur den Dateinamen für Referenzen
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[PDF Upload] PDF content saved to database for invoice {invoice.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PDF Upload] ERROR saving PDF content: {ex.Message}");
                throw new InvalidOperationException($"Failed to save PDF content: {ex.Message}", ex);
            }

            // Lösche die temporäre Datei
            try
            {
                System.IO.File.Delete(tempPath);
                Console.WriteLine($"[PDF Upload] Temporary file deleted: {tempPath}");
            }
            catch (Exception)
            {
                // Best-effort cleanup; ignore delete failures
            }

            Console.WriteLine($"[PDF Upload] Upload completed successfully for invoice {invoice.Id}");
            return invoice;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDF Upload] FATAL ERROR: {ex.Message}");
            Console.WriteLine($"[PDF Upload] Stack trace: {ex.StackTrace}");
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
        catch (Exception)
        {
            
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

            return true;
        }
        catch (Exception)
        {
            
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
        catch (Exception)
        {
            
            throw;
        }
    }

    private ExtractedInvoiceData ExtractInvoiceDataFromPdf(string filePath)
    {
        try
        {
            Console.WriteLine($"[PDF Extract] Starting extraction from: {filePath}");
            var extractedData = new ExtractedInvoiceData();
            string text = string.Empty;
            
            using (var pdfReader = new PdfReader(filePath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                var strategy = new LocationTextExtractionStrategy();
                
                // Extrahiere Text von allen Seiten
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    text += PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy);
                }

                Console.WriteLine($"[PDF Extract] Extracted text length: {text.Length} characters");
                
                // Falls kein Text extrahiert wurde, versuche OCR
                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine($"[PDF Extract] No text found, trying OCR...");
                    text = ExtractTextWithOcr(filePath);
                    Console.WriteLine($"[OCR] Extracted text length: {text.Length} characters");
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Extrahiere Rechnungsnummer
                    extractedData.InvoiceNumber = ExtractInvoiceNumberFromText(text);
                    Console.WriteLine($"[PDF Extract] Invoice Number: {extractedData.InvoiceNumber}");
                    
                    // Extrahiere Beträge
                    var amounts = ExtractAmounts(text);
                    extractedData.NetAmount = amounts.Net;
                    extractedData.TaxAmount = amounts.Tax;
                    extractedData.TotalAmount = amounts.Total;
                    Console.WriteLine($"[PDF Extract] Amounts - Net: {amounts.Net}, Tax: {amounts.Tax}, Total: {amounts.Total}");
                    
                    // Extrahiere Datum
                    extractedData.InvoiceDate = ExtractInvoiceDate(text);
                    extractedData.DueDate = ExtractDueDate(text);
                    Console.WriteLine($"[PDF Extract] Dates - Invoice: {extractedData.InvoiceDate}, Due: {extractedData.DueDate}");
                    
                    // Extrahiere Währung
                    extractedData.Currency = ExtractCurrency(text);
                    Console.WriteLine($"[PDF Extract] Currency: {extractedData.Currency}");
                    
                    // Extrahiere Lieferanteninformation
                    extractedData.SupplierInfo = ExtractSupplierInfo(text);
                    Console.WriteLine($"[PDF Extract] Supplier: {extractedData.SupplierInfo?.Name}, VAT: {extractedData.SupplierInfo?.VatNumber}");
                    
                    // Erstelle Beschreibung aus ersten Zeilen
                    var lines = text.Split('\n').Take(5).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
                    extractedData.Description = string.Join(" | ", lines);
                    if (extractedData.Description.Length > 500)
                    {
                        extractedData.Description = extractedData.Description.Substring(0, 497) + "...";
                    }
                    Console.WriteLine($"[PDF Extract] Description: {extractedData.Description}");
                }
                else
                {
                    Console.WriteLine($"[PDF Extract] WARNING: No text extracted even with OCR");
                }
            }
            
            Console.WriteLine($"[PDF Extract] Extraction completed successfully");
            return extractedData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDF Extract] ERROR during extraction: {ex.Message}");
            Console.WriteLine($"[PDF Extract] Stack trace: {ex.StackTrace}");
            return new ExtractedInvoiceData();
        }
    }

    private string ExtractTextWithOcr(string pdfFilePath)
    {
        try
        {
            Console.WriteLine($"[OCR] Starting OCR extraction from: {pdfFilePath}");
            
            // Prüfe ob Tesseract-Sprachdaten vorhanden sind
            var germanDataFile = Path.Combine(_tessdataPath, "deu.traineddata");
            var italianDataFile = Path.Combine(_tessdataPath, "ita.traineddata");
            
            if (!File.Exists(germanDataFile) && !File.Exists(italianDataFile))
            {
                Console.WriteLine($"[OCR] ERROR: No language data files found in {_tessdataPath}");
                Console.WriteLine($"[OCR] Please download deu.traineddata and ita.traineddata from: https://github.com/tesseract-ocr/tessdata");
                return string.Empty;
            }

            // Bestimme verfügbare Sprachen
            var languages = new List<string>();
            if (File.Exists(germanDataFile)) languages.Add("deu");
            if (File.Exists(italianDataFile)) languages.Add("ita");
            var langString = string.Join("+", languages);
            
            Console.WriteLine($"[OCR] Using Tesseract with languages: {langString}");

            // Konvertiere PDF zu Bildern und führe OCR aus
            var extractedText = new System.Text.StringBuilder();

            // Lade PDF in MemoryStream und konvertiere zu Bildern
            List<SkiaSharp.SKBitmap> imageList;
            using (var pdfStream = new MemoryStream())
            {
                using (var fileStream = File.OpenRead(pdfFilePath))
                {
                    fileStream.CopyTo(pdfStream);
                }
                pdfStream.Position = 0;
                
                Console.WriteLine($"[OCR] Converting PDF to images...");
                
                // Konvertiere PDF-Seiten zu SKBitmaps (300 DPI) und lade sofort
#pragma warning disable CA1416
                imageList = Conversion.ToImages(pdfStream, options: new(Dpi: 300)).Take(10).ToList();
#pragma warning restore CA1416
                Console.WriteLine($"[OCR] PDF converted to {imageList.Count} image(s)");
            }

            // Verarbeite die Bilder mit Tesseract (außerhalb des using-Blocks)
            try
            {
                using (var engine = new TesseractEngine(_tessdataPath, langString, EngineMode.Default))
                {
                    Console.WriteLine($"[OCR] Tesseract engine initialized");
                    
                    for (int i = 0; i < imageList.Count; i++)
                    {
                        try
                        {
                            Console.WriteLine($"[OCR] Processing page {i + 1}/{imageList.Count}...");
                            
                            var skBitmap = imageList[i];
                            
                            // Konvertiere SKBitmap zu byte array für Tesseract
                            using (var ms = new MemoryStream())
                            {
                                skBitmap.Encode(ms, SkiaSharp.SKEncodedImageFormat.Png, 100);
                                byte[] imageBytes = ms.ToArray();
                                
                                // OCR auf dem Bild durchführen
                                using (var pix = Pix.LoadFromMemory(imageBytes))
                                using (var page = engine.Process(pix))
                                {
                                    var pageText = page.GetText();
                                    extractedText.AppendLine(pageText);
                                    Console.WriteLine($"[OCR] Page {i + 1}: Extracted {pageText.Length} characters (confidence: {page.GetMeanConfidence():P0})");
                                }
                            }
                            
                            // Dispose SKBitmap
                            skBitmap.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[OCR] Error processing page {i + 1}: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                // Cleanup: Dispose alle Bitmaps
                foreach (var bitmap in imageList)
                {
                    bitmap?.Dispose();
                }
            }

            var result = extractedText.ToString();
            Console.WriteLine($"[OCR] OCR completed. Total extracted: {result.Length} characters");
            
            // Removed verbose OCR preview logging
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OCR] ERROR during OCR extraction: {ex.Message}");
            Console.WriteLine($"[OCR] Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }

    private string? ExtractInvoiceNumberFromText(string text)
    {
        // Suche nach verschiedenen Rechnungsnummer-Mustern
        var patterns = new[]
        {
            @"Numero\s+fattura[\s\r\n]+(\d+\-\d+)",  // "Numero fattura 12-139856"
            @"Numero\s+fattura[^\d]{0,30}(\d{2}\-\d{3,})", // erlaubt Trennzeichen (|, -, .) zwischen Label und Nummer
            @"CONTO\s+LINKEM\s*[—-]\s*(\d{2}\-\d{3,})",   // "CONTO LINKEM — 12-139856"
            @"(?:Dok\.|Dok\.\s+N|Dok\s+N)[°º\-\.?]?\s*\-?\s*N[°º\-\.?]?\s*([A-Z0-9\-]+)",
            @"\bDok[°º\-\.?]?\s*\-?\s*N[°º\-\.?]\s*([A-Z0-9\-]+)",
            @"\b(S\-\d{3,})\b",  // Wichtig: S-XXX als Ganzes extrahieren
            @"(?:Rechnung(?:snummer)?|Rechnungs-Nr)[:\s]+([A-Z0-9\-\/]+)",
            @"(?:Rechnung\s+nr\.?|Fattura\s+n[°º]?)\s*(\d{3,})",
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
                    Console.WriteLine($"[PDF Extract] Found invoice number: {invoiceNumber}");
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
        bool foundExplicitInvoiceAmount = false;

        // Suche zuerst nach dem Rechnungsbetrag direkt (am wichtigsten!)
        var invoiceAmountPatterns = new[]
        {
            @"TOTALE\s+CONTO\s+LINKEM[\s\r\n]+([\d]+[.,]\d{2})",  // "TOTALE CONTO LINKEM 80,73"
            @"TOTALE\s+(?:CONTO|fattura|Rechnung)[\s\r\n]+([\d]+[.,]\d{2})",
            @"Euro\s+(\d+[.,]\d{2})", // z.B. "è di Euro 80,73"
            @"([\d]+[.,]\d{2})\s*€\s*$",  // Betrag mit € am Zeilenende (höchste Priorität)
            @"Gesamtbetrag\s+Importo\s+totale[\s\S]*?([\d]+[.,]\d{2})\s*€",  // Letzter Betrag nach "Gesamtbetrag Importo totale"
            @"Rechnungsbetrag[^\d]+([\d\s]+[.,]\d{2})\s*(?:euro|EUR|€)",
            @"Rechnungsbetrag[^\d]+([\d\s]+[.,]\d{2})",
            @"Gesamtbetrag[^\d]+([\d\s]+[.,]\d{2})",
            @"Importo\s+totale[^\d]+([\d\s]+[.,]\d{2})",
            @"(?:Betrag|Amount)[:\s]+€?\s*([\d\s]+[.,]\d{2})"
        };

        foreach (var pattern in invoiceAmountPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success && match.Groups.Count > 1)
            {
                var parsed = ParseAmount(match.Groups[1].Value);
                if (parsed.HasValue && parsed.Value > 10)
                {
                    totalAmount = parsed.Value;
                    foundExplicitInvoiceAmount = true;
                    Console.WriteLine($"[PDF Extract] Found invoice amount pattern: {parsed.Value}");
                    break;
                }
            }
        }

        // Fallback: Finde alle Beträge (mit und ohne € Symbol)
        // Robust Betrags-Erkennung: nicht erlauben, dass ein Betrag direkt nach einer Ziffer oder einem Bindestrich kommt (z.B. Teil einer Rechnungsnummer)
        var allAmountsPattern = @"(?<![\d\-/])(?:€\s*)?(\d+(?:[.,]\d{3})*[.,]\d{2})(?!\d)";
        var amountMatches = Regex.Matches(text, allAmountsPattern);
        var foundAmounts = new List<(string text, decimal value, int position)>();

        foreach (Match match in amountMatches)
        {
            if (match.Groups.Count > 1)
            {
                var amountStr = match.Groups[1].Value;
                var parsedAmount = ParseAmount(amountStr);
                if (parsedAmount.HasValue && parsedAmount.Value > 10) // Ignoriere sehr kleine Beträge
                {
                    foundAmounts.Add((amountStr, parsedAmount.Value, match.Index));
                }
            }
        }

        // Suche nach spezifischen Betrags-Labels als Fallback (nur wenn expliziter Rechnungsbetrag nicht gefunden)
        if (!foundExplicitInvoiceAmount && !totalAmount.HasValue)
        {
            var specificPatterns = new[]
            {
                @"Totale\s+Dokument\s+\(Totale\s+Documento\)\s+([\d\s]+[.,]\d{2})",
                @"(?:Totale|Total)\s+Documento[^\d]+([\d\s]+[.,]\d{2})",
                @"(?:Totale|Total)\s*\(Totale\)\s+([\d\s]+[.,]\d{2})",
                @"Totale\s+fattura:\s*€?\s*([\d\s]+[.,]\d{2})",
                @"(?:Gesamt|Total)[:\s]+€?\s*([\d\s]+[.,]\d{2})",
                @"(?:Summe|Sum|Somma)[:\s]+€?\s*([\d\s]+[.,]\d{2})"
            };

            foreach (var pattern in specificPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var parsed = ParseAmount(match.Groups[1].Value);
                    if (parsed.HasValue)
                    {
                        totalAmount = parsed.Value;
                        break;
                    }
                }
            }
        }

        // WICHTIG: Nur wenn KEIN expliziter Rechnungsbetrag gefunden wurde, darf mit foundAmounts verglichen werden
        if (!foundExplicitInvoiceAmount)
        {
            // Wenn wir einen Betrag gefunden haben, aber es gibt einen deutlich größeren Betrag
            if (totalAmount.HasValue && foundAmounts.Any())
            {
                var maxAmount = foundAmounts.OrderByDescending(x => x.value).First();
                if (maxAmount.value > totalAmount.Value * 1.1m) // Mindestens 10% größer
                {
                    totalAmount = maxAmount.value;
                }
            }

            // Wenn nichts gefunden, verwende größten Betrag
            if (!totalAmount.HasValue && foundAmounts.Any())
            {
                var maxAmount = foundAmounts.OrderByDescending(x => x.value).First();
                totalAmount = maxAmount.value;
            }
        }
        else if (!totalAmount.HasValue)
        {
            // Letzter Fallback: Suche nach "Totale fattura:" Muster
            var totalPatterns = new[]
            {
                @"Totale\s+fattura:\s*€?\s*(\d+[.,]\d{2})",
                @"Totale\s+fattura:\s*€?\s*(\d+(?:[.,]\d{3})*[.,]\d{2})"
            };

            foreach (var pattern in totalPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var amountStr = match.Groups[1].Value.Trim();
                    
                    totalAmount = ParseAmount(amountStr);
                    if (totalAmount.HasValue)
                    {
                        
                        break;
                    }
                }
            }
        }

        // Suche nach Nettobetrag
        var netPatterns = new[]
        {
            @"(?:Netto|Net|Imponibile|Subtotal)[:\s]+€?\s*(\d+[.,]\d{2})",
            @"(?:Zwischensumme)[:\s]+€?\s*(\d+[.,]\d{2})"
        };

        foreach (var pattern in netPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                netAmount = ParseAmount(match.Groups[1].Value.Trim());
                if (netAmount.HasValue)
                {
                    
                    break;
                }
            }
        }

        // Suche nach Steuerbetrag oder Steuersatz
        // Erst versuchen, den tatsächlichen Betrag zu finden
        var taxAmountPatterns = new[]
        {
            @"(?:IVA|MwSt|USt|VAT)[:\s]*€?\s*(\d+[.,]\d{2})",
            @"€\s*(\d+[.,]\d{2})\s*(?:IVA|MwSt|USt|VAT)",
            @"Steuer[:\s]+€?\s*(\d+[.,]\d{2})"
        };

        foreach (var pattern in taxAmountPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var potentialTaxAmount = ParseAmount(match.Groups[1].Value.Trim());
                if (potentialTaxAmount.HasValue)
                {
                    taxAmount = potentialTaxAmount;
                    
                    break;
                }
            }
        }

        // Wenn nur Gesamtbetrag vorhanden, berechne Netto und Steuer
        if (totalAmount.HasValue && (!netAmount.HasValue || !taxAmount.HasValue))
        {
            // Suche nach IVA-Prozentsatz
            var ivaMatch = Regex.Match(text, @"IVA\s+(\d{1,2})\%", RegexOptions.IgnoreCase);
            if (ivaMatch.Success && int.TryParse(ivaMatch.Groups[1].Value, out int ivaPct))
            {
                decimal factor = 1 + (ivaPct / 100m);
                netAmount = totalAmount.Value / factor;
                taxAmount = totalAmount.Value - netAmount.Value;
                
            }
            else
            {
                // Suche nach MwSt-Prozentsatz
                var mwstMatch = Regex.Match(text, @"(?:MwSt|USt)\s+(\d{1,2})\%", RegexOptions.IgnoreCase);
                if (mwstMatch.Success && int.TryParse(mwstMatch.Groups[1].Value, out int mwstPct))
                {
                    decimal factor = 1 + (mwstPct / 100m);
                    netAmount = totalAmount.Value / factor;
                    taxAmount = totalAmount.Value - netAmount.Value;
                    
                }
                else
                {
                    // Fallback: Annahme 19% MwSt
                    netAmount = totalAmount.Value / 1.19m;
                    taxAmount = totalAmount.Value - netAmount.Value;
                    
                }
            }
        }

        return (netAmount, taxAmount, totalAmount);
    }

    private decimal? ParseAmount(string amountStr)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(amountStr))
            {
                
                return null;
            }

            // Entferne Leerzeichen (z.B. "81 9,19" → "819,19")
            amountStr = Regex.Replace(amountStr, @"\s+", "");
            
            // Entferne Währungssymbole und führende/nachfolgende Spaces
            amountStr = Regex.Replace(amountStr, @"[€$£\s]", "").Trim();

            // Bestimme, ob Punkt oder Komma als Dezimaltrennzeichen verwendet wird
            var lastComma = amountStr.LastIndexOf(',');
            var lastDot = amountStr.LastIndexOf('.');
            
            if (lastComma > lastDot)
            {
                // Deutsches Format: 1.234,56
                amountStr = amountStr.Replace(".", "").Replace(",", ".");
                
            }
            else if (lastDot > lastComma && lastDot >= 0)
            {
                // Englisches Format: 1,234.56 oder 500.00
                amountStr = amountStr.Replace(",", "");
                
            }
            
            if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                
                return result;
            }
            else
            {
                
            }
        }
        catch (Exception)
        {
            
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

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var currentYear = DateTime.UtcNow.Year;
        
        // Finde die höchste Nummer des aktuellen Jahres
        var invoicesThisYear = await _context.Invoices
            .Where(i => i.InvoiceNumber.EndsWith(currentYear.ToString()))
            .Select(i => i.InvoiceNumber)
            .ToListAsync();

        int nextNumber = 1;
        
        if (invoicesThisYear.Any())
        {
            // Extrahiere die Nummer aus bestehenden Rechnungsnummern (Format: FAT-023-2025)
            var numbers = invoicesThisYear
                .Select(inv => 
                {
                    var parts = inv.Split('-');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int num))
                    {
                        return num;
                    }
                    return 0;
                })
                .Where(n => n > 0)
                .ToList();

            if (numbers.Any())
            {
                nextNumber = numbers.Max() + 1;
            }
        }

        // Generiere Rechnungsnummer im Format FAT-{Nummer mit führenden Nullen}-{Jahr}
        return $"FAT-{nextNumber:D3}-{currentYear}";
    }

    private async Task<int> FindOrCreateSupplierAsync(SupplierInfo? supplierInfo, string tempPdfPath)
    {
        if (supplierInfo == null || string.IsNullOrWhiteSpace(supplierInfo.Name))
        {
            
            // Suche oder erstelle einen Standard-Lieferanten
            var defaultSupplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Unbekannter Lieferant");
            if (defaultSupplier == null)
            {
                defaultSupplier = new Supplier
                {
                    Name = "Unbekannter Lieferant",
                    Country = "Deutschland"
                };
                _context.Suppliers.Add(defaultSupplier);
                await _context.SaveChangesAsync();
                
            }
            return defaultSupplier.Id;
        }

        // Suche nach existierendem Lieferanten
        var existingSupplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => 
                s.Name.ToLower() == supplierInfo.Name.ToLower() ||
                (supplierInfo.VatNumber != null && s.VatNumber == supplierInfo.VatNumber) ||
                (supplierInfo.TaxNumber != null && s.TaxNumber == supplierInfo.TaxNumber));

        if (existingSupplier != null)
        {
            
            return existingSupplier.Id;
        }

        // Erstelle neuen Lieferanten
        var newSupplier = new Supplier
        {
            Name = supplierInfo.Name,
            LegalName = supplierInfo.LegalName ?? supplierInfo.Name,
            VatNumber = supplierInfo.VatNumber,
            TaxNumber = supplierInfo.TaxNumber,
            AddressLine1 = supplierInfo.Address,
            City = supplierInfo.City,
            PostalCode = supplierInfo.PostalCode,
            Country = supplierInfo.Country ?? "Italien"
        };

        _context.Suppliers.Add(newSupplier);
        await _context.SaveChangesAsync();

        return newSupplier.Id;
    }

    private SupplierInfo? ExtractSupplierInfo(string text)
    {
        var supplierInfo = new SupplierInfo();

        // Spezialfall: "CONTO LINKEM" deutet auf Linkem als Lieferant hin
        if (Regex.IsMatch(text, @"CONTO\s+LINKEM", RegexOptions.IgnoreCase))
        {
            var linkemMatch = Regex.Match(text, @"LINKEM\s+S\.P\.A\.\s*-\s*Sede\s+(?:Legale|Operativa)", RegexOptions.IgnoreCase);
            if (linkemMatch.Success)
            {
                supplierInfo.Name = "LINKEM S.P.A.";
                Console.WriteLine($"[PDF Extract] Found supplier name: LINKEM S.P.A.");
                
                // Suche nach P.IVA
                var pivaMatch = Regex.Match(text, @"P\.IVA\s+(\d{11})", RegexOptions.IgnoreCase);
                if (pivaMatch.Success)
                {
                    supplierInfo.VatNumber = pivaMatch.Groups[1].Value;
                    supplierInfo.TaxNumber = supplierInfo.VatNumber;
                }
                
                return supplierInfo;
            }
        }

        // WICHTIG: Finde die Position nach der die Kundenadresse beginnt
        // Nach Tel/Fax kommt normalerweise die Kundenadresse, nicht der Lieferant
        var kundenPosition = -1;
        
        // Suche nach "Tel." gefolgt von einer Person/Adresse (nicht mehr Firma)
        var telMatch = Regex.Match(text, @"Tel\.\s+\+?\d+.*?(?=\n\s*[A-Z][a-z]+\s+[A-Z][a-z]+\s*\n)", RegexOptions.Singleline);
        if (telMatch.Success)
        {
            kundenPosition = telMatch.Index + telMatch.Length;
            Console.WriteLine($"[PDF Extract] Found customer section start at position {kundenPosition}");
        }
        
        // Alternativ: Suche nach expliziten Kundenlabels
        if (kundenPosition < 0)
        {
            var kundenMatch = Regex.Match(text, @"\b(Kunde(?:ndaten)?|Cliente|Kund[ae]n-Nr|Customer|Bestimmungsort|Cod\.\s+Cliente)\b", RegexOptions.IgnoreCase);
            if (kundenMatch.Success)
            {
                kundenPosition = kundenMatch.Index;
                Console.WriteLine($"[PDF Extract] Found customer label at position {kundenPosition}");
            }
        }

        // Suche nach Firmenname in ersten Zeilen (typisch bei Rechnungen)
        // Muster für Firmennamen mit optionalen Großbuchstaben am Anfang
        var firstLinesPattern = @"^\s*([A-Za-zÄÖÜäöü][A-Za-zÄÖÜäöü0-9\s&\-\.®©]+(?:GMBH|GmbH|SMBH|GMBH & CO\.? KG|AG|SRL|SPA|INC|LLC|LTD)?)[\s®©:]*[\r\n]";
        var firstLineMatches = Regex.Matches(text, firstLinesPattern, RegexOptions.Multiline);
        
        foreach (Match match in firstLineMatches)
        {
            var name = match.Groups[1].Value.Trim();
            
            // Prüfe ob dieser Name VOR der Kundenposition ist
            if (kundenPosition > 0 && match.Index >= kundenPosition)
            {
                Console.WriteLine($"[PDF Extract] Skipping '{name}' - appears after customer section");
                continue; // Überspringe Namen die nach "Kunde" kommen
            }
            
            // Ignoriere Personennamen (Vorname Nachname) - das sind Kunden, keine Firmen
            if (Regex.IsMatch(name, @"^[A-Z][a-z]+\s+[A-Z][a-z]+$"))
            {
                Console.WriteLine($"[PDF Extract] Skipping '{name}' - looks like a person name");
                continue;
            }
            
            if (!string.IsNullOrEmpty(name) && name.Length > 3)
            {
                supplierInfo.Name = name;
                Console.WriteLine($"[PDF Extract] Found supplier name: {name}");
                break;
            }
        }

        // Suche nach Fornitore (Italienisch für Lieferant/Anbieter)
        if (string.IsNullOrWhiteSpace(supplierInfo.Name))
        {
            var fornitoPattern = @"Fornitore\s+(.*?)(?=\n|Cliente|P\.\s*IVA)";
            var fornitoMatch = Regex.Match(text, fornitoPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (fornitoMatch.Success)
            {
                var fornitoText = fornitoMatch.Groups[1].Value.Trim();
                var lines = fornitoText.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
                
                if (lines.Length > 0)
                {
                    supplierInfo.Name = lines[0];

                    // Versuche Adresse zu finden
                    if (lines.Length > 1)
                    {
                        supplierInfo.Address = lines[1];
                    }
                }
            }
        }

        // Fallback: Suche nach Name am Anfang des Dokuments (erste paar Zeilen nach "FATTURA")
        if (string.IsNullOrWhiteSpace(supplierInfo.Name))
        {
            var namePattern = @"FATTURA\s+(?:Numero fattura:.*?\s+Data:.*?\s+Fornitore\s+)?([\w\s]+(?:SRL|SpA|GmbH|AG|Inc|LLC|Ltd))";
            var nameMatch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (nameMatch.Success)
            {
                supplierInfo.Name = nameMatch.Groups[1].Value.Trim();
                
            }
        }

        // Suche nach P. IVA oder P.Iva (Italienische Umsatzsteuer-ID)
        var vatPatterns = new[]
        {
            @"P\.\s*[Ii]va:\s*([A-Z]{2}\d{11}|\d{11})",
            @"P\.Iva[:\s]+([A-Z]{2}\d{11}|\d{11})",
            @"VAT[:\s]+([A-Z]{2}\d{9,15})"
        };
        
        foreach (var pattern in vatPatterns)
        {
            var vatMatch = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (vatMatch.Success)
            {
                supplierInfo.VatNumber = vatMatch.Groups[1].Value.Trim();
                supplierInfo.TaxNumber = supplierInfo.VatNumber;
                break;
            }
        }

        // Suche nach Adresse und PLZ/Stadt
        var addressPattern = @"Via\s+([\w\s]+)\s+(\d+)";
        var addressMatch = Regex.Match(text, addressPattern, RegexOptions.IgnoreCase);
        if (addressMatch.Success)
        {
            supplierInfo.Address = $"Via {addressMatch.Groups[1].Value.Trim()} {addressMatch.Groups[2].Value}";
            
        }

        // Suche nach PLZ und Stadt (italienisches Format: 00100 Roma)
        var cityPattern = @"(\d{5})\s+([A-Za-zàèéìòù]+(?:\s+\([A-Z]{2}\))?)";
        var cityMatch = Regex.Match(text, cityPattern);
        if (cityMatch.Success)
        {
            supplierInfo.PostalCode = cityMatch.Groups[1].Value;
            supplierInfo.City = cityMatch.Groups[2].Value.Replace("(RM)", "").Replace("(MI)", "").Trim();
            
        }

        // Bestimme Land basierend auf Indizien
        if (!string.IsNullOrWhiteSpace(supplierInfo.VatNumber) || text.Contains("P. IVA") || text.Contains("Fattura"))
        {
            supplierInfo.Country = "Italien";
        }
        else if (text.Contains("MwSt") || text.Contains("USt"))
        {
            supplierInfo.Country = "Deutschland";
        }

        return string.IsNullOrWhiteSpace(supplierInfo.Name) ? null : supplierInfo;
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
    public SupplierInfo? SupplierInfo { get; set; }
}

public class SupplierInfo
{
    public string? Name { get; set; }
    public string? LegalName { get; set; }
    public string? VatNumber { get; set; }
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}
