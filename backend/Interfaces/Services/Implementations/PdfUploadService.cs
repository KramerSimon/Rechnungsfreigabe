using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using System.Globalization;
using Tesseract;
using PDFtoImage;
using System.Xml;
using System.Xml.Linq;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class PdfUploadService : IPdfUploadService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IInvoiceService invoiceService;
    private readonly ILogger<PdfUploadService> logger;
    private readonly string uploadDirectory;
    private readonly string tessdataPath;
    private readonly long maxFileSize = 50 * 1024 * 1024; // 50 MB
    private readonly string[] allowedExtensions = { ".pdf", ".xml" };

    public PdfUploadService(
        IUnitOfWork unitOfWork,
        IInvoiceService invoiceService,
        IWebHostEnvironment webHostEnvironment,
        ILogger<PdfUploadService> logger)
    {
        this.unitOfWork = unitOfWork;
        this.invoiceService = invoiceService;
        this.logger = logger;
        uploadDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "uploads", "invoices");
        tessdataPath = Path.Combine(webHostEnvironment.ContentRootPath, "tessdata");
        
        // Stelle sicher, dass das Upload-Verzeichnis existiert
        if (!Directory.Exists(uploadDirectory))
        {
            Directory.CreateDirectory(uploadDirectory);
        }

        // Stelle sicher, dass das Tessdata-Verzeichnis existiert
        if (!Directory.Exists(tessdataPath))
        {
            Directory.CreateDirectory(tessdataPath);
            logger.LogInformation("Created tessdata directory at: {TessdataPath}", tessdataPath);
            logger.LogInformation("Please download language files from: https://github.com/tesseract-ocr/tessdata");
            logger.LogInformation("Required: deu.traineddata (German), ita.traineddata (Italian)");
        }
    }

    public async Task<InvoiceDto> UploadInvoicePdfAsync(IFormFile file, int? supplierId, string? purchaseOrderId, string? costCenterId, string? projectId, int userId)
    {
        try
        {
            logger.LogInformation("[PDF Upload] Starting upload for file: {FileName}, Size: {Length} bytes", file.FileName, file.Length);

            // Validiere die Datei
            ValidateFile(file);
            logger.LogInformation("[PDF Upload] File validation passed");

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isXml = fileExtension == ".xml";

            // Validiere dass der User existiert
            var userExists = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId) != null;
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }
            logger.LogInformation("[PDF Upload] User {UserId} validated", userId);

            // Validiere dass Cost Center, Project und Purchase Order zusammenpassen
            await ValidateProjectPurchaseOrderRelationship(costCenterId, projectId, purchaseOrderId);

            // Lies die Datei in den Speicher
            byte[] pdfContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                pdfContent = memoryStream.ToArray();
            }
            logger.LogInformation("[PDF Upload] File content loaded into memory: {Length} bytes", pdfContent.Length);

            // Generiere eindeutigen Dateinamen
            var fileName = GenerateUniqueFileName(file.FileName);
            logger.LogInformation("[PDF Upload] Generated unique filename: {FileName}", fileName);

            // Extrahiere Daten aus dem Dokument
            var tempPath = Path.Combine(uploadDirectory, fileName);
            ExtractedInvoiceData extractedData;
            try
            {
                await using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await stream.WriteAsync(pdfContent, 0, pdfContent.Length);
                }
                logger.LogInformation("[PDF Upload] Temporary file created at: {TempPath}", tempPath);

                extractedData = isXml ? ExtractInvoiceDataFromXml(tempPath) : ExtractInvoiceDataFromPdf(tempPath);
                logger.LogInformation("[PDF Upload] Data extracted - InvoiceNum: {InvoiceNumber}, Total: {TotalAmount}, Supplier: {SupplierName}", extractedData.InvoiceNumber, extractedData.TotalAmount, extractedData.SupplierInfo?.Name);
            }
            finally
            {
                TryDeleteTempFile(tempPath, "[PDF Upload]");
            }

            // Generiere Rechnungsnummer im Format FAT-{Nummer}-{Jahr}
            var invoiceNumber = await GenerateInvoiceNumberAsync();
            logger.LogInformation("[PDF Upload] Generated invoice number: {InvoiceNumber}", invoiceNumber);

            // Finde oder erstelle Lieferant
            int finalSupplierId;
            if (supplierId.HasValue && supplierId.Value > 0)
            {
                // Verwende die �bergebene SupplierId
                var supplier = await unitOfWork.Suppliers.GetByIdAsync(supplierId.Value);
                if (supplier == null)
                {
            logger.LogInformation("[PDF Upload] ERROR: Supplier with ID {SupplierId} not found", supplierId);
                    throw new InvalidOperationException($"Supplier with ID {supplierId} not found");
                }
                finalSupplierId = supplierId.Value;
            logger.LogInformation("[PDF Upload] Using provided supplier ID: {FinalSupplierId}", finalSupplierId);
            }
            else
            {
                // Extrahiere und erstelle/finde Lieferant aus PDF
                finalSupplierId = await FindOrCreateSupplierAsync(extractedData.SupplierInfo, tempPath);
            logger.LogInformation("[PDF Upload] Found/created supplier ID: {FinalSupplierId}", finalSupplierId);
            }

            // Validiere Purchase Order ID falls angegeben
            string? validatedPurchaseOrderId = null;
            if (!string.IsNullOrWhiteSpace(purchaseOrderId))
            {
                var purchaseOrderExists = await unitOfWork.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == purchaseOrderId) != null;
                if (!purchaseOrderExists)
                {
                    // Log warnung aber blockiere nicht - setze einfach null
            logger.LogInformation("[PDF Upload] Warning: Purchase Order '{PurchaseOrderId}' not found, setting to null", purchaseOrderId);
                }
                else
                {
                    validatedPurchaseOrderId = purchaseOrderId;
            logger.LogInformation("[PDF Upload] Validated Purchase Order: {ValidatedPurchaseOrderId}", validatedPurchaseOrderId);
                }
            }

            // Validiere Cost Center ID falls angegeben
            string? validatedCostCenterId = null;
            if (!string.IsNullOrWhiteSpace(costCenterId))
            {
                var costCenterExists = await unitOfWork.CostCenters.FirstOrDefaultAsync(cc => cc.Id == costCenterId) != null;
                if (!costCenterExists)
                {
                    // Log warnung aber blockiere nicht - setze einfach null
            logger.LogInformation("[PDF Upload] Warning: Cost Center '{CostCenterId}' not found, setting to null", costCenterId);
                }
                else
                {
                    validatedCostCenterId = costCenterId;
            logger.LogInformation("[PDF Upload] Validated Cost Center: {ValidatedCostCenterId}", validatedCostCenterId);
                }
            }

            // Erstelle Invoice-Eintrag in der Datenbank mit extrahierten Daten
            var createInvoiceDto = new CreateInvoiceDto
            {
                InvoiceNumber = invoiceNumber,
                SupplierId = finalSupplierId,
                PurchaseOrderId = validatedPurchaseOrderId,
                CostCenterId = validatedCostCenterId,
                ProjectId = projectId,
                NetAmount = extractedData.NetAmount ?? 0,
                TaxAmount = extractedData.TaxAmount ?? 0,
                TotalAmount = extractedData.TotalAmount ?? 0,
                Currency = extractedData.Currency ?? "EUR",
                InvoiceDate = extractedData.InvoiceDate ?? DateTime.UtcNow,
                DueDate = extractedData.DueDate ?? DateTime.UtcNow.AddDays(30),
                RequiresApproval = true,
                Description = extractedData.Description ?? $"{(isXml ? "XML" : "PDF")}-Upload: {file.FileName}"
            };

            logger.LogInformation("[PDF Upload] Creating invoice with data - Number: {InvoiceNumber}, Total: {TotalAmount}, Net: {NetAmount}, Tax: {TaxAmount}", createInvoiceDto.InvoiceNumber, createInvoiceDto.TotalAmount, createInvoiceDto.NetAmount, createInvoiceDto.TaxAmount);

            InvoiceDto invoice;
            try
            {
                invoice = await invoiceService.CreateInvoiceAsync(createInvoiceDto, userId);
            logger.LogInformation("[PDF Upload] Invoice created successfully with ID: {InvoiceId}", invoice.Id);
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
            logger.LogInformation("[PDF Upload] ERROR creating invoice: {FullMessage}", fullMessage);
                throw new InvalidOperationException(fullMessage, ex);
            }

            // Speichere PDF-Content direkt in der Datenbank (kein dauerhaftes Filesystem mehr)
            try
            {
                var invoiceDb = await unitOfWork.Invoices.GetByIdAsync(invoice.Id);
                if (invoiceDb != null)
                {
                    invoiceDb.PdfContent = pdfContent;
                    invoiceDb.PdfFileSize = file.Length;
                    invoiceDb.OriginalFilename = file.FileName;
                    invoiceDb.PdfFilePath = null; // keine lokale Ablage mehr
                    await unitOfWork.SaveChangesAsync();
            logger.LogInformation("[PDF Upload] File content saved to database for invoice {InvoiceId}", invoice.Id);
                }
            }
            catch (Exception ex)
            {
            logger.LogInformation("[PDF Upload] ERROR saving PDF content: {Message}", ex.Message);
                throw new InvalidOperationException($"Failed to save PDF content: {ex.Message}", ex);
            }

            logger.LogInformation("[PDF Upload] Upload completed successfully for invoice {InvoiceId}", invoice.Id);
            return invoice;
        }
        catch (Exception ex)
        {
            logger.LogInformation("[PDF Upload] FATAL ERROR: {Message}", ex.Message);
            logger.LogInformation("[PDF Upload] Stack trace: {StackTrace}", ex.StackTrace);
            throw;
        }
    }

    public async Task<PurchaseOrderDto> UploadPurchaseOrderPdfAsync(IFormFile file, int? supplierId, string? costCenterId, string? projectId, int userId)
    {
        try
        {
            logger.LogInformation("[PO PDF Upload] Starting upload for file: {FileName}, Size: {Length} bytes", file.FileName, file.Length);

            // Validiere die Datei
            ValidateFile(file);
            logger.LogInformation("[PO PDF Upload] File validation passed");

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isXml = fileExtension == ".xml";

            // Validiere dass der User existiert
            var userExists = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId) != null;
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }
            logger.LogInformation("[PO PDF Upload] User {UserId} validated", userId);

            // Lies die Datei in den Speicher
            byte[] pdfContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                pdfContent = memoryStream.ToArray();
            }
            logger.LogInformation("[PO PDF Upload] File content loaded into memory: {Length} bytes", pdfContent.Length);

            // Generiere eindeutigen Dateinamen
            var fileName = GenerateUniqueFileName(file.FileName);
            logger.LogInformation("[PO PDF Upload] Generated unique filename: {FileName}", fileName);

            // Extrahiere Daten aus dem Dokument
            var tempPath = Path.Combine(uploadDirectory, fileName);
            ExtractedInvoiceData extractedData;
            try
            {
                await using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await stream.WriteAsync(pdfContent, 0, pdfContent.Length);
                }
                logger.LogInformation("[PO PDF Upload] Temporary file created at: {TempPath}", tempPath);

                extractedData = isXml ? ExtractInvoiceDataFromXml(tempPath) : ExtractInvoiceDataFromPdf(tempPath);
                logger.LogInformation("[PO PDF Upload] Data extracted - Total: {TotalAmount}, Supplier: {SupplierName}", extractedData.TotalAmount, extractedData.SupplierInfo?.Name);
            }
            finally
            {
                TryDeleteTempFile(tempPath, "[PO PDF Upload]");
            }

            // Generiere Purchase Order ID im Format PO-{Nummer}-{Jahr}
            var poId = await GeneratePurchaseOrderIdAsync();
            logger.LogInformation("[PO PDF Upload] Generated PO ID: {PoId}", poId);

            // Finde oder erstelle Lieferant
            int? finalSupplierId = null;
            if (supplierId.HasValue && supplierId.Value > 0)
            {
                // Verwende die �bergebene SupplierId
                var supplier = await unitOfWork.Suppliers.GetByIdAsync(supplierId.Value);
                if (supplier == null)
                {
            logger.LogInformation("[PO PDF Upload] ERROR: Supplier with ID {SupplierId} not found", supplierId);
                    throw new InvalidOperationException($"Supplier with ID {supplierId} not found");
                }
                finalSupplierId = supplierId.Value;
            logger.LogInformation("[PO PDF Upload] Using provided supplier ID: {FinalSupplierId}", finalSupplierId);
            }
            else if (extractedData.SupplierInfo != null)
            {
                // Extrahiere und erstelle/finde Lieferant aus PDF
                finalSupplierId = await FindOrCreateSupplierAsync(extractedData.SupplierInfo, tempPath);
            logger.LogInformation("[PO PDF Upload] Found/created supplier ID: {FinalSupplierId}", finalSupplierId);
            }

            // Validiere Cost Center ID - REQUIRED
            if (string.IsNullOrWhiteSpace(costCenterId))
            {
            logger.LogInformation("[PO PDF Upload] ERROR: Cost Center is required");
                throw new InvalidOperationException("Cost Center is required for purchase order uploads");
            }

            var costCenterExists = await unitOfWork.CostCenters.FirstOrDefaultAsync(cc => cc.Id == costCenterId) != null;
            if (!costCenterExists)
            {
            logger.LogInformation("[PO PDF Upload] ERROR: Cost Center '{CostCenterId}' not found", costCenterId);
                throw new InvalidOperationException($"Cost Center '{costCenterId}' not found");
            }
            logger.LogInformation("[PO PDF Upload] Validated Cost Center: {CostCenterId}", costCenterId);

            // Validiere Project ID - REQUIRED
            if (string.IsNullOrWhiteSpace(projectId))
            {
            logger.LogInformation("[PO PDF Upload] ERROR: Project is required");
                throw new InvalidOperationException("Project is required for purchase order uploads");
            }

            var project = await unitOfWork.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
            {
            logger.LogInformation("[PO PDF Upload] ERROR: Project '{ProjectId}' not found", projectId);
                throw new InvalidOperationException($"Project '{projectId}' not found");
            }

            // Validate that project belongs to the selected cost center
            if (project.CostCenterId != costCenterId)
            {
            logger.LogInformation("[PO PDF Upload] ERROR: Project '{ProjectId}' does not belong to Cost Center '{CostCenterId}'. Project belongs to '{ProjectCostCenterId}'", projectId, costCenterId, project.CostCenterId);
                throw new InvalidOperationException($"Project '{projectId}' does not belong to the selected Cost Center. The project belongs to Cost Center '{project.CostCenterId}'");
            }
            logger.LogInformation("[PO PDF Upload] Validated Project: {ProjectId}, belongs to Cost Center: {CostCenterId}", projectId, costCenterId);

            // Erstelle Purchase Order mit extrahierten Daten
            var offenStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.PurchaseOrder.Offen,
                EntityTypes.PurchaseOrder);
            
            var purchaseOrder = new PurchaseOrder
            {
                Id = poId,
                Title = poId,  // Use order number as title
                Description = extractedData.Description,
                SupplierId = finalSupplierId,
                CostCenterId = costCenterId,  // Required
                ProjectId = projectId,  // Required
                TotalAmount = extractedData.TotalAmount ?? 0,
                Currency = extractedData.Currency ?? "EUR",
                StatusId = offenStatus?.Id,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                PdfContent = pdfContent,
                PdfFileSize = file.Length,
                OriginalFilename = file.FileName
            };

            logger.LogInformation("[PO PDF Upload] Creating purchase order - ID: {Id}, Total: {TotalAmount}", purchaseOrder.Id, purchaseOrder.TotalAmount);

            unitOfWork.PurchaseOrders.Add(purchaseOrder);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("[PO PDF Upload] Purchase Order created successfully with ID: {Id}", purchaseOrder.Id);

            // Lade vollst�ndige PO mit Navigations-Properties
            var po = await unitOfWork.PurchaseOrders.GetByNumberAsync(poId);

            if (po == null)
            {
                throw new InvalidOperationException("Failed to retrieve created purchase order");
            }

            var poDto = new PurchaseOrderDto
            {
                Id = po.Id,
                Title = po.Title,
                Description = po.Description,
                CostCenterId = po.CostCenterId,
                CostCenterName = po.CostCenter?.Name,
                ProjectId = po.ProjectId,
                ProjectName = po.Project?.Name,
                TotalAmount = po.TotalAmount,
                Currency = po.Currency,
                Status = po.Status?.ToString() ?? string.Empty,
                Creator = po.Creator != null ? new UserDto
                {
                    Id = po.Creator.Id,
                    FirstName = po.Creator.FirstName,
                    LastName = po.Creator.LastName,
                    Email = po.Creator.Email
                } : null,
                Approver = po.Approver != null ? new UserDto
                {
                    Id = po.Approver.Id,
                    FirstName = po.Approver.FirstName,
                    LastName = po.Approver.LastName,
                    Email = po.Approver.Email
                } : null,
                CreatedAt = po.CreatedAt,
                ApprovedAt = po.ApprovedAt
            };

            logger.LogInformation("[PO PDF Upload] Upload completed successfully for PO {PoId}", poDto.Id);
            return poDto;
        }
        catch (Exception ex)
        {
            logger.LogInformation("[PO PDF Upload] FATAL ERROR: {Message}", ex.Message);
            logger.LogInformation("[PO PDF Upload] Stack trace: {StackTrace}", ex.StackTrace);
            throw;
        }
    }

    public async Task<byte[]> GetInvoicePdfAsync(int invoiceId)
    {
        try
        {
            var invoice = await unitOfWork.Invoices.GetByIdAsync(invoiceId);
            if (invoice?.PdfContent == null || invoice.PdfContent.Length == 0)
            {
                throw new FileNotFoundException($"PDF for invoice {invoiceId} not found in database");
            }

            return invoice.PdfContent;
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
            var invoice = await unitOfWork.Invoices.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                return false;
            }

            // Entferne ausschlie�lich die DB-Inhalte
            invoice.PdfContent = null;
            invoice.PdfFilePath = null;
            invoice.PdfFileSize = null;
            invoice.OriginalFilename = null;
            await unitOfWork.SaveChangesAsync();

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
            var allInvoices = (await unitOfWork.Invoices.GetAllAsync()).ToList();
            
            var totalInvoices = allInvoices.Count;
            var invoicesWithPdf = allInvoices.Count(i => i.PdfContent != null && i.PdfContent.Length > 0);
            var totalPdfSize = allInvoices
                .Where(i => i.PdfFileSize.HasValue)
                .Sum(i => i.PdfFileSize!.Value);

            return new PdfUploadStatusDto
            {
                TotalInvoices = totalInvoices,
                InvoicesWithPdf = invoicesWithPdf,
                TotalPdfSize = totalPdfSize,
                DiskUsageBytes = 0,
                UploadDirectory = "database",
                MaxFileSize = maxFileSize
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void TryDeleteTempFile(string? tempPath, string logPrefix)
    {
        if (string.IsNullOrWhiteSpace(tempPath))
        {
            return;
        }

        try
        {
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
                logger.LogInformation("{LogPrefix} Temporary file deleted: {TempPath}", logPrefix, tempPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation("{LogPrefix} Failed to delete temporary file: {TempPath}. Error: {Error}", logPrefix, tempPath, ex.Message);
        }
    }

    private ExtractedInvoiceData ExtractInvoiceDataFromPdf(string filePath)
    {
        try
        {
            logger.LogInformation("[PDF Extract] Starting extraction from: {FilePath}", filePath);
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

            logger.LogInformation("[PDF Extract] Extracted text length: {Length} characters", text.Length);
                
                // Falls kein Text extrahiert wurde, versuche OCR
                if (string.IsNullOrWhiteSpace(text))
                {
            logger.LogInformation("[PDF Extract] No text found, trying OCR...");
                    text = ExtractTextWithOcr(filePath);
            logger.LogInformation("[OCR] Extracted text length: {TextLength} characters", text.Length);
                    
                    // Debug: Print first 500 characters to see what OCR extracted
                    if (text.Length > 0)
                    {
                        var preview = text.Length > 500 ? text.Substring(0, 500) : text;
            logger.LogInformation("[OCR] Text preview: {PreviewText}", preview.Replace("\n", " | ").Replace("\r", ""));
                    }
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Extrahiere Rechnungsnummer
                    extractedData.InvoiceNumber = ExtractInvoiceNumberFromText(text);
            logger.LogInformation("[PDF Extract] Invoice Number: {InvoiceNumber}", extractedData.InvoiceNumber);
                    
                    // Extrahiere Betr�ge
                    var amounts = ExtractAmounts(text);
                    extractedData.NetAmount = amounts.Net;
                    extractedData.TaxAmount = amounts.Tax;
                    extractedData.TotalAmount = amounts.Total;
            logger.LogInformation("[PDF Extract] Amounts - Net: {Net}, Tax: {Tax}, Total: {Total}", amounts.Net, amounts.Tax, amounts.Total);
                    
                    // Extrahiere Datum
                    extractedData.InvoiceDate = ExtractInvoiceDate(text);
                    extractedData.DueDate = ExtractDueDate(text);
            logger.LogInformation("[PDF Extract] Dates - Invoice: {InvoiceDate}, Due: {DueDate}", extractedData.InvoiceDate, extractedData.DueDate);
                    
                    // Extrahiere W�hrung
                    extractedData.Currency = ExtractCurrency(text);
            logger.LogInformation("[PDF Extract] Currency: {Currency}", extractedData.Currency);
                    
                    // Extrahiere Lieferanteninformation
                    extractedData.SupplierInfo = ExtractSupplierInfo(text);
            logger.LogInformation("[PDF Extract] Supplier: {SupplierName}, VAT: {VatNumber}", extractedData.SupplierInfo?.Name, extractedData.SupplierInfo?.VatNumber);
                    
                    // Erstelle Beschreibung aus ersten Zeilen
                    var lines = text.Split('\n').Take(5).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
                    extractedData.Description = string.Join(" | ", lines);
                    if (extractedData.Description.Length > 500)
                    {
                        extractedData.Description = extractedData.Description.Substring(0, 497) + "...";
                    }
            logger.LogInformation("[PDF Extract] Description: {Description}", extractedData.Description);
                }
                else
                {
            logger.LogInformation("[PDF Extract] WARNING: No text extracted even with OCR");
                }
            }
            
            logger.LogInformation("[PDF Extract] Extraction completed successfully");
            return extractedData;
        }
        catch (Exception ex)
        {
            logger.LogInformation("[PDF Extract] ERROR during extraction: {Message}", ex.Message);
            logger.LogInformation("[PDF Extract] Stack trace: {StackTrace}", ex.StackTrace);
            return new ExtractedInvoiceData();
        }
    }

    private ExtractedInvoiceData ExtractInvoiceDataFromXml(string filePath)
    {
        try
        {
            logger.LogInformation("[XML Extract] Starting extraction from: {FilePath}", filePath);
            var extractedData = new ExtractedInvoiceData();

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var reader = XmlReader.Create(filePath, settings);
            var doc = XDocument.Load(reader);
            var root = doc.Root;

            if (root == null)
            {
                logger.LogInformation("[XML Extract] WARNING: No root element found");
                return extractedData;
            }

            extractedData.InvoiceNumber = FindInvoiceNumberFromXml(root);
            extractedData.InvoiceDate = ParseXmlDate(FindFirstValue(root, "Data", "IssueDate", "InvoiceDate", "IssueDateTime", "Date"));
            extractedData.DueDate = ParseXmlDate(FindFirstValue(root, "DataScadenzaPagamento", "DueDate", "PaymentDueDate", "PaymentDueDateTime"));

            decimal? totalAmount = null;
            string? currency = FindFirstValue(root, "Divisa", "Currency", "CurrencyCode");

            var legalMonetaryTotal = root.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, "LegalMonetaryTotal", StringComparison.OrdinalIgnoreCase));
            if (legalMonetaryTotal != null)
            {
                var payable = FindAmount(legalMonetaryTotal, "PayableAmount", "GrandTotalAmount", "TotalAmount");
                totalAmount = payable.Amount;
                currency = payable.Currency;

                var net = FindAmount(legalMonetaryTotal, "TaxExclusiveAmount", "LineExtensionAmount");
                extractedData.NetAmount = net.Amount;
                extractedData.Currency = currency ?? net.Currency;
            }

            if (!totalAmount.HasValue)
            {
                totalAmount = ParseXmlAmount(FindFirstValue(root, "ImportoTotaleDocumento"));
            }

            if (!totalAmount.HasValue)
            {
                var fallbackTotal = FindAmount(root, "PayableAmount", "GrandTotalAmount", "TotalAmount", "Amount");
                totalAmount = fallbackTotal.Amount;
                currency = currency ?? fallbackTotal.Currency;
            }

            var tax = FindAmount(root, "Imposta", "TaxAmount");
            extractedData.TaxAmount = tax.Amount;
            extractedData.TotalAmount = totalAmount;
            extractedData.Currency = extractedData.Currency ?? currency ?? tax.Currency;

            extractedData.NetAmount = extractedData.NetAmount ?? ParseXmlAmount(FindFirstValue(root, "ImponibileImporto"));

            extractedData.Description = FindFirstValue(root, "Causale", "Descrizione", "Note", "Description", "DocumentDescription");
            extractedData.SupplierInfo = ExtractSupplierInfoFromXml(root);

            logger.LogInformation("[XML Extract] Extraction completed successfully");
            return extractedData;
        }
        catch (Exception ex)
        {
            logger.LogInformation("[XML Extract] ERROR during extraction: {Message}", ex.Message);
            logger.LogInformation("[XML Extract] Stack trace: {StackTrace}", ex.StackTrace);
            return new ExtractedInvoiceData();
        }
    }

    private string? FindInvoiceNumberFromXml(XElement root)
    {
        var fatturaNumero = root.Descendants().FirstOrDefault(e =>
            string.Equals(e.Name.LocalName, "Numero", StringComparison.OrdinalIgnoreCase));
        var fatturaNumeroValue = fatturaNumero?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(fatturaNumeroValue))
        {
            return fatturaNumeroValue;
        }

        var invoiceId = root.Descendants().FirstOrDefault(e =>
            string.Equals(e.Name.LocalName, "ID", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(e.Parent?.Name.LocalName, "Invoice", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(e.Parent?.Name.LocalName, "ExchangedDocument", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(e.Parent?.Name.LocalName, "Document", StringComparison.OrdinalIgnoreCase)));

        var value = invoiceId?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return FindFirstValue(root, "InvoiceNumber", "InvoiceNo", "ID", "Numero");
    }

    private SupplierInfo? ExtractSupplierInfoFromXml(XElement root)
    {
        var supplierParty = root.Descendants().FirstOrDefault(e =>
            string.Equals(e.Name.LocalName, "CedentePrestatore", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "AccountingSupplierParty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "SellerSupplierParty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "SellerTradeParty", StringComparison.OrdinalIgnoreCase));

        if (supplierParty == null)
        {
            return null;
        }

        var supplierInfo = new SupplierInfo
        {
            Name = FindFirstValue(supplierParty, "Denominazione", "RegistrationName", "Name", "PartyName", "CompanyName"),
            LegalName = FindFirstValue(supplierParty, "Denominazione", "RegistrationName", "Name"),
            VatNumber = FindFirstValue(supplierParty, "IdCodice", "VATIdentifier", "VATRegistrationNumber", "VATID", "CompanyID"),
            TaxNumber = FindFirstValue(supplierParty, "CodiceFiscale", "TaxNumber")
        };

        var street = FindFirstValue(supplierParty, "Indirizzo", "StreetName", "LineOne", "Line", "Street");
        var buildingNumber = FindFirstValue(supplierParty, "NumeroCivico", "BuildingNumber", "HouseNumber");
        var additional = FindFirstValue(supplierParty, "AdditionalStreetName");
        var addressParts = new[] { street, buildingNumber, additional }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        supplierInfo.Address = string.Join(" ", addressParts);

        supplierInfo.PostalCode = FindFirstValue(supplierParty, "CAP", "PostalZone", "PostcodeCode", "PostalCode");
        supplierInfo.City = FindFirstValue(supplierParty, "Comune", "CityName", "City");
        supplierInfo.Country = FindFirstValue(supplierParty, "Nazione", "IdentificationCode", "CountryID", "CountryName");

        return string.IsNullOrWhiteSpace(supplierInfo.Name) ? null : supplierInfo;
    }

    private (decimal? Amount, string? Currency) FindAmount(XContainer root, params string[] localNames)
    {
        foreach (var name in localNames)
        {
            var element = root.Descendants().FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
            if (element == null)
            {
                continue;
            }

            var amount = ParseXmlAmount(element.Value);
            var currency = element.Attribute("currencyID")?.Value
                           ?? element.Attribute("currency")?.Value
                           ?? element.Attribute("currencyCode")?.Value;

            if (amount.HasValue || !string.IsNullOrWhiteSpace(currency))
            {
                return (amount, currency);
            }
        }

        return (null, null);
    }

    private decimal? ParseXmlAmount(string? amountStr)
    {
        if (string.IsNullOrWhiteSpace(amountStr))
        {
            return null;
        }

        if (decimal.TryParse(amountStr.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return ParseAmount(amountStr);
    }

    private DateTime? ParseXmlDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return null;
        }

        if (TryParseDate(dateStr.Trim(), out var parsed))
        {
            return parsed;
        }

        if (DateTime.TryParse(dateStr.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? FindFirstValue(XContainer root, params string[] localNames)
    {
        foreach (var name in localNames)
        {
            var element = root.Descendants().FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
            var value = element?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private string ExtractTextWithOcr(string pdfFilePath)
    {
        try
        {
            logger.LogInformation("[OCR] Starting OCR extraction from: {PdfFilePath}", pdfFilePath);
            
            // Pr�fe ob Tesseract-Sprachdaten vorhanden sind
            var germanDataFile = Path.Combine(tessdataPath, "deu.traineddata");
            var italianDataFile = Path.Combine(tessdataPath, "ita.traineddata");
            
            if (!File.Exists(germanDataFile) && !File.Exists(italianDataFile))
            {
            logger.LogInformation("[OCR] ERROR: No language data files found in {TessdataPath}", tessdataPath);
            logger.LogInformation("[OCR] Please download deu.traineddata and ita.traineddata from: https://github.com/tesseract-ocr/tessdata");
                return string.Empty;
            }

            // Bestimme verf�gbare Sprachen
            var languages = new List<string>();
            if (File.Exists(germanDataFile)) languages.Add("deu");
            if (File.Exists(italianDataFile)) languages.Add("ita");
            var langString = string.Join("+", languages);
            
            logger.LogInformation("[OCR] Using Tesseract with languages: {LangString}", langString);

            // Konvertiere PDF zu Bildern und f�hre OCR aus
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
                
            logger.LogInformation("[OCR] Converting PDF to images...");
                
                // Konvertiere PDF-Seiten zu SKBitmaps (300 DPI) und lade sofort
#pragma warning disable CA1416
                imageList = Conversion.ToImages(pdfStream, options: new(Dpi: 300)).Take(10).ToList();
#pragma warning restore CA1416
            logger.LogInformation("[OCR] PDF converted to {Count} image(s)", imageList.Count);
            }

            // Verarbeite die Bilder mit Tesseract (au�erhalb des using-Blocks)
            try
            {
                using (var engine = new TesseractEngine(tessdataPath, langString, EngineMode.Default))
                {
            logger.LogInformation("[OCR] Tesseract engine initialized");
                    
                    for (int i = 0; i < imageList.Count; i++)
                    {
                        try
                        {
            logger.LogInformation("[OCR] Processing page {Page}/{Total}...", i + 1, imageList.Count);
                            
                            var skBitmap = imageList[i];
                            
                            // Konvertiere SKBitmap zu byte array f�r Tesseract
                            using (var ms = new MemoryStream())
                            {
                                skBitmap.Encode(ms, SkiaSharp.SKEncodedImageFormat.Png, 100);
                                byte[] imageBytes = ms.ToArray();
                                
                                // OCR auf dem Bild durchf�hren
                                using (var pix = Pix.LoadFromMemory(imageBytes))
                                using (var page = engine.Process(pix))
                                {
                                    var pageText = page.GetText();
                                    extractedText.AppendLine(pageText);
            logger.LogInformation("[OCR] Page {Page}: Extracted {Length} characters (confidence: {Confidence:P0})", i + 1, pageText.Length, page.GetMeanConfidence());
                                }
                            }
                            
                            // Dispose SKBitmap
                            skBitmap.Dispose();
                        }
                        catch (Exception ex)
                        {
            logger.LogInformation("[OCR] Error processing page {Page}: {Message}", i + 1, ex.Message);
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
            logger.LogInformation("[OCR] OCR completed. Total extracted: {Length} characters", result.Length);
            
            // Removed verbose OCR preview logging
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogInformation("[OCR] ERROR during OCR extraction: {Message}", ex.Message);
            logger.LogInformation("[OCR] Stack trace: {StackTrace}", ex.StackTrace);
            return string.Empty;
        }
    }

    private string? ExtractInvoiceNumberFromText(string text)
    {
        // Suche nach verschiedenen Rechnungsnummer-Mustern
        var patterns = new[]
        {
            @"Rechnung\s+Nr\.?\s*([A-Z]{3}\d{10})",  // "Rechnung Nr. AEL2400398577" (Alperia)
            @"Nr\.?\s*([A-Z]{3}\d{10})\b",  // "Nr. AEL2400398577" or "Nr AEL2400398577"
            @"\b([A-Z]{3}\d{10})\b",  // Generic pattern for Alperia invoice numbers
            @"Numero\s+fattura[\s\r\n]+(\d+\-\d+)",  // "Numero fattura 12-139856"
            @"Numero\s+fattura[^\d]{0,30}(\d{2}\-\d{3,})", // erlaubt Trennzeichen (|, -, .) zwischen Label und Nummer
            @"CONTO\s+LINKEM\s*[�-]\s*(\d{2}\-\d{3,})",   // "CONTO LINKEM � 12-139856"
            @"(?:Dok\.|Dok\.\s+N|Dok\s+N)[��\-\.?]?\s*\-?\s*N[��\-\.?]?\s*([A-Z0-9\-]+)",
            @"\bDok[��\-\.?]?\s*\-?\s*N[��\-\.?]\s*([A-Z0-9\-]+)",
            @"\b(S\-\d{3,})\b",  // Wichtig: S-XXX als Ganzes extrahieren
            @"(?:Rechnung(?:snummer)?|Rechnungs-Nr)[:\s]+([A-Z0-9\-\/]+)",
            @"(?:Rechnung\s+nr\.?|Fattura\s+n[��]?)\s*(\d{3,})",
            @"(?:Rechnung(?:snummer)?|Invoice(?:\s+Number)?|R(?:ech)?\.?\s*Nr\.?)[:\s]+([A-Z0-9\-\/]+)",
            @"(?:Fattura|Factura)\s+N[��.\s]*([A-Z0-9\-\/]+)",
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
            logger.LogInformation("[PDF Extract] Found invoice number: {InvoiceNumber}", invoiceNumber);
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
            @"Rechnungsbetrag[\s\r\n]+([\d]+\s*[.,]\s*\d{2})\s*euro",  // "Rechnungsbetrag 200,39 euro" or "200 , 39 euro" (Alperia)
            @"([\d]{2,}[.,]\d{2})\s*euro",  // Any amount followed by "euro" (Alperia fallback)
            @"TOTALE\s+CONTO\s+LINKEM[\s\r\n]+([\d]+[.,]\d{2})",  // "TOTALE CONTO LINKEM 80,73"
            @"TOTALE\s+(?:CONTO|fattura|Rechnung)[\s\r\n]+([\d]+[.,]\d{2})",
            @"Euro\s+(\d+[.,]\d{2})", // z.B. "� di Euro 80,73"
            @"([\d]+[.,]\d{2})\s*�\s*$",  // Betrag mit � am Zeilenende (h�chste Priorit�t)
            @"Gesamtbetrag\s+Importo\s+totale[\s\S]*?([\d]+[.,]\d{2})\s*�",  // Letzter Betrag nach "Gesamtbetrag Importo totale"
            @"Rechnungsbetrag[^\d]+([\d\s]+[.,]\d{2})\s*(?:euro|EUR|�)",
            @"Rechnungsbetrag[^\d]+([\d\s]+[.,]\d{2})",
            @"Gesamtbetrag[^\d]+([\d\s]+[.,]\d{2})",
            @"Importo\s+totale[^\d]+([\d\s]+[.,]\d{2})",
            @"(?:Betrag|Amount)[:\s]+�?\s*([\d\s]+[.,]\d{2})"
        };

        foreach (var pattern in invoiceAmountPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success && match.Groups.Count > 1)
            {
                var parsed = ParseAmount(match.Groups[1].Value);
                if (parsed.HasValue && parsed.Value > 1) // Lowered threshold from 10 to 1
                {
                    totalAmount = parsed.Value;
                    foundExplicitInvoiceAmount = true;
            logger.LogInformation("[PDF Extract] Found invoice amount pattern: {Value}", parsed.Value);
                    break;
                }
            }
        }

        // Fallback: Finde alle Betr�ge (mit und ohne � Symbol)
        // Robust Betrags-Erkennung: nicht erlauben, dass ein Betrag direkt nach einer Ziffer oder einem Bindestrich kommt (z.B. Teil einer Rechnungsnummer)
        var allAmountsPattern = @"(?<![\d\-/])(?:�\s*)?(\d+(?:[.,]\d{3})*[.,]\d{2})(?!\d)";
        var amountMatches = Regex.Matches(text, allAmountsPattern);
        var foundAmounts = new List<(string text, decimal value, int position)>();

        foreach (Match match in amountMatches)
        {
            if (match.Groups.Count > 1)
            {
                var amountStr = match.Groups[1].Value;
                var parsedAmount = ParseAmount(amountStr);
                if (parsedAmount.HasValue && parsedAmount.Value > 10) // Ignoriere sehr kleine Betr�ge
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
                @"Totale\s+fattura:\s*�?\s*([\d\s]+[.,]\d{2})",
                @"(?:Gesamt|Total)[:\s]+�?\s*([\d\s]+[.,]\d{2})",
                @"(?:Summe|Sum|Somma)[:\s]+�?\s*([\d\s]+[.,]\d{2})"
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
            // Wenn wir einen Betrag gefunden haben, aber es gibt einen deutlich gr��eren Betrag
            if (totalAmount.HasValue && foundAmounts.Any())
            {
                var maxAmount = foundAmounts.OrderByDescending(x => x.value).First();
                if (maxAmount.value > totalAmount.Value * 1.1m) // Mindestens 10% gr��er
                {
                    totalAmount = maxAmount.value;
                }
            }

            // Wenn nichts gefunden, verwende gr��ten Betrag
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
                @"Totale\s+fattura:\s*�?\s*(\d+[.,]\d{2})",
                @"Totale\s+fattura:\s*�?\s*(\d+(?:[.,]\d{3})*[.,]\d{2})"
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
            @"(?:Netto|Net|Imponibile|Subtotal)[:\s]+�?\s*(\d+[.,]\d{2})",
            @"(?:Zwischensumme)[:\s]+�?\s*(\d+[.,]\d{2})"
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
        // Erst versuchen, den tats�chlichen Betrag zu finden
        var taxAmountPatterns = new[]
        {
            @"(?:IVA|MwSt|USt|VAT)[:\s]*�?\s*(\d+[.,]\d{2})",
            @"�\s*(\d+[.,]\d{2})\s*(?:IVA|MwSt|USt|VAT)",
            @"Steuer[:\s]+�?\s*(\d+[.,]\d{2})"
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

            // Entferne Leerzeichen (z.B. "81 9,19" ? "819,19")
            amountStr = Regex.Replace(amountStr, @"\s+", "");
            
            // Entferne W�hrungssymbole und f�hrende/nachfolgende Spaces
            amountStr = Regex.Replace(amountStr, @"[�$�\s]", "").Trim();

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
            @"innerhalb[\s\r\n]+(\d{1,2})[\/](\d{1,2})[\/](\d{4})",  // "innerhalb 13/11/2024" (Alperia)
            @"(?:F�lligkeitsdatum|Due\s+Date|Scadenza)[:\s]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})",
            @"(?:Zahlbar\s+bis|Payment\s+due)[:\s]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})"
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // Handle captured groups for day/month/year separately (Alperia format)
                if (match.Groups.Count > 3 && int.TryParse(match.Groups[1].Value, out int day) 
                    && int.TryParse(match.Groups[2].Value, out int month) 
                    && int.TryParse(match.Groups[3].Value, out int year))
                {
                    try
                    {
                        return new DateTime(year, month, day);
                    }
                    catch { }
                }
                // Standard single group capture
                else if (match.Groups.Count > 1 && TryParseDate(match.Groups[1].Value, out DateTime date))
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
        if (text.Contains("�") || Regex.IsMatch(text, @"\bEUR\b", RegexOptions.IgnoreCase))
            return "EUR";
        if (text.Contains("$") || Regex.IsMatch(text, @"\bUSD\b", RegexOptions.IgnoreCase))
            return "USD";
        if (text.Contains("�") || Regex.IsMatch(text, @"\bGBP\b", RegexOptions.IgnoreCase))
            return "GBP";
        if (text.Contains("CHF") || Regex.IsMatch(text, @"\bCHF\b", RegexOptions.IgnoreCase))
            return "CHF";
            
        return "EUR"; // Default
    }

    private async Task ValidateProjectPurchaseOrderRelationship(string? costCenterId, string? projectId, string? purchaseOrderId)
    {
        // Wenn projectId vorhanden ist, validiere dass es zur Kostenstelle passt
        if (!string.IsNullOrEmpty(projectId))
        {
            var project = await unitOfWork.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
            {
                throw new ArgumentException($"Project with ID '{projectId}' not found");
            }

            // Validiere dass das Projekt zur Kostenstelle geh�rt
            if (!string.IsNullOrEmpty(costCenterId) && project.CostCenterId != costCenterId)
            {
                throw new ArgumentException($"Project '{projectId}' does not belong to Cost Center '{costCenterId}'");
            }

            // Aktualisiere costCenterId basierend auf Project, wenn nicht vorhanden
            if (string.IsNullOrEmpty(costCenterId))
            {
                costCenterId = project.CostCenterId;
            }
        }

        // Wenn purchaseOrderId vorhanden ist, validiere dass es zur Kostenstelle und Projekt passt
        if (!string.IsNullOrEmpty(purchaseOrderId))
        {
            var purchaseOrder = await unitOfWork.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
            if (purchaseOrder == null)
            {
                throw new ArgumentException($"Purchase Order with ID '{purchaseOrderId}' not found");
            }

            // Validiere dass die Bestellung zur Kostenstelle geh�rt
            if (!string.IsNullOrEmpty(costCenterId) && purchaseOrder.CostCenterId != costCenterId)
            {
                throw new ArgumentException($"Purchase Order '{purchaseOrderId}' does not belong to Cost Center '{costCenterId}'");
            }

            // Validiere dass die Bestellung zum Projekt geh�rt (wenn Projekt angegeben ist)
            if (!string.IsNullOrEmpty(projectId) && purchaseOrder.ProjectId != projectId)
            {
                throw new ArgumentException($"Purchase Order '{purchaseOrderId}' does not belong to Project '{projectId}'");
            }
        }
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        if (file.Length > maxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)} MB");
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException($"File type '{fileExtension}' is not allowed. Only PDF or XML files are allowed.");
        }

        if (fileExtension == ".pdf")
        {
            // Pr�fe Magic Bytes f�r PDF
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
        else if (fileExtension == ".xml")
        {
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                using var reader = XmlReader.Create(file.OpenReadStream(), settings);
                while (reader.Read())
                {
                    // Just iterate to validate
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"File is not a valid XML file: {ex.Message}");
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
        
        // Finde die h�chste Nummer des aktuellen Jahres
        var allInvoices = await unitOfWork.Invoices.GetAllAsync();
        var invoicesThisYear = allInvoices
            .Where(i => i.InvoiceNumber.EndsWith(currentYear.ToString()))
            .Select(i => i.InvoiceNumber)
            .ToList();

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

        // Generiere Rechnungsnummer im Format FAT-{Nummer mit f�hrenden Nullen}-{Jahr}
        return $"FAT-{nextNumber:D3}-{currentYear}";
    }

    private async Task<string> GeneratePurchaseOrderIdAsync()
    {
        var currentYear = DateTime.UtcNow.Year;
        
        // Finde die h�chste Nummer des aktuellen Jahres
        var allPOs = await unitOfWork.PurchaseOrders.GetAllAsync();
        var posThisYear = allPOs
            .Where(po => po.Id.EndsWith(currentYear.ToString()))
            .Select(po => po.Id)
            .ToList();

        int nextNumber = 1;
        
        if (posThisYear.Any())
        {
            // Extrahiere die Nummer aus bestehenden PO IDs (Format: PO-023-2025)
            var numbers = posThisYear
                .Select(poId => 
                {
                    var parts = poId.Split('-');
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

        // Generiere PO ID im Format PO-{Nummer mit f�hrenden Nullen}-{Jahr}
        return $"PO-{nextNumber:D3}-{currentYear}";
    }

    private async Task<int> FindOrCreateSupplierAsync(SupplierInfo? supplierInfo, string tempPdfPath)
    {
        if (supplierInfo == null || string.IsNullOrWhiteSpace(supplierInfo.Name))
        {
            
            // Suche oder erstelle einen Standard-Lieferanten
            var defaultSupplier = await unitOfWork.Suppliers.FirstOrDefaultAsync(s => s.Name == "Unbekannter Lieferant");
            if (defaultSupplier == null)
            {
                defaultSupplier = new Supplier
                {
                    Name = "Unbekannter Lieferant",
                    Country = "Deutschland"
                };
                unitOfWork.Suppliers.Add(defaultSupplier);
                await unitOfWork.SaveChangesAsync();
                
            }
            return defaultSupplier.Id;
        }

        // Suche nach existierendem Lieferanten
        var allSuppliers = await unitOfWork.Suppliers.GetAllAsync();
        var existingSupplier = allSuppliers.FirstOrDefault(s => 
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
            Name = supplierInfo.Name?.Length > 100 ? supplierInfo.Name.Substring(0, 100) : supplierInfo.Name ?? "Unknown",
            LegalName = (supplierInfo.LegalName ?? supplierInfo.Name)?.Length > 150 
                ? (supplierInfo.LegalName ?? supplierInfo.Name)!.Substring(0, 150) 
                : supplierInfo.LegalName ?? supplierInfo.Name,
            VatNumber = supplierInfo.VatNumber?.Length > 30 ? supplierInfo.VatNumber.Substring(0, 30) : supplierInfo.VatNumber,
            TaxNumber = supplierInfo.TaxNumber?.Length > 30 ? supplierInfo.TaxNumber.Substring(0, 30) : supplierInfo.TaxNumber,
            AddressLine1 = supplierInfo.Address?.Length > 100 ? supplierInfo.Address.Substring(0, 100) : supplierInfo.Address,
            City = supplierInfo.City?.Length > 50 ? supplierInfo.City.Substring(0, 50) : supplierInfo.City,
            PostalCode = supplierInfo.PostalCode?.Length > 10 ? supplierInfo.PostalCode.Substring(0, 10) : supplierInfo.PostalCode,
            Country = (supplierInfo.Country ?? "Italien").Length > 50 ? (supplierInfo.Country ?? "Italien").Substring(0, 50) : supplierInfo.Country ?? "Italien"
        };

        unitOfWork.Suppliers.Add(newSupplier);
        await unitOfWork.SaveChangesAsync();

        return newSupplier.Id;
    }

    private SupplierInfo? ExtractSupplierInfo(string text)
    {
        var supplierInfo = new SupplierInfo();

        // Spezialfall: Alperia - check for "ALPERIA" brand
        if (Regex.IsMatch(text, @"\bALPERIA\b", RegexOptions.IgnoreCase))
        {
            supplierInfo.Name = "ALPERIA";
            logger.LogInformation("[PDF Extract] Found supplier name: ALPERIA");
            
            // Alperia hat normalerweise keine explizite P.IVA im Hauptbereich
            supplierInfo.Country = "Italien";
            
            return supplierInfo;
        }

        // Spezialfall: "CONTO LINKEM" deutet auf Linkem als Lieferant hin
        if (Regex.IsMatch(text, @"CONTO\s+LINKEM", RegexOptions.IgnoreCase))
        {
            var linkemMatch = Regex.Match(text, @"LINKEM\s+S\.P\.A\.\s*-\s*Sede\s+(?:Legale|Operativa)", RegexOptions.IgnoreCase);
            if (linkemMatch.Success)
            {
                supplierInfo.Name = "LINKEM S.P.A.";
            logger.LogInformation("[PDF Extract] Found supplier name: LINKEM S.P.A.");
                
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
            logger.LogInformation("[PDF Extract] Found customer section start at position {KundenPosition}", kundenPosition);
        }
        
        // Alternativ: Suche nach expliziten Kundenlabels
        if (kundenPosition < 0)
        {
            var kundenMatch = Regex.Match(text, @"\b(Kunde(?:ndaten)?|Cliente|Kund[ae]n-Nr|Customer|Bestimmungsort|Cod\.\s+Cliente)\b", RegexOptions.IgnoreCase);
            if (kundenMatch.Success)
            {
                kundenPosition = kundenMatch.Index;
            logger.LogInformation("[PDF Extract] Found customer label at position {KundenPosition}", kundenPosition);
            }
        }

        // 1) Bevorzugt: expliziter Fornitore-Block nutzen
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
                    var cleaned = CleanSupplierName(lines[0]);
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        supplierInfo.Name = cleaned.Length > 100 ? cleaned.Substring(0, 100) : cleaned;
                    }

                    // Versuche Adresse zu finden
                    if (lines.Length > 1)
                    {
                        var address = lines[1];
                        supplierInfo.Address = address.Length > 100 ? address.Substring(0, 100) : address;
                    }
                }
            }
        }

        // 2) Firmenname in ersten Zeilen (typisch bei Rechnungen)
        if (string.IsNullOrWhiteSpace(supplierInfo.Name))
        {
            var firstLinesPattern = @"^\s*([A-Za-z������][A-Za-z������0-9\s&\-\.��]+(?:GMBH|GmbH|SMBH|GMBH & CO\.? KG|AG|SRL|SPA|INC|LLC|LTD)?)[\s��:]*[\r\n]";
            var firstLineMatches = Regex.Matches(text, firstLinesPattern, RegexOptions.Multiline);
            
            foreach (Match match in firstLineMatches)
            {
                var rawName = match.Groups[1].Value.Trim();
                var name = CleanSupplierName(rawName);
                
                if (string.IsNullOrWhiteSpace(name))
                {
            logger.LogInformation("[PDF Extract] Skipping '{RawName}' - filtered out", rawName);
                    continue;
                }

                // Pr�fe ob dieser Name VOR der Kundenposition ist
                if (kundenPosition > 0 && match.Index >= kundenPosition)
                {
            logger.LogInformation("[PDF Extract] Skipping '{Name}' - appears after customer section", name);
                    continue; // �berspringe Namen die nach "Kunde" kommen
                }
                
                // Ignoriere Personennamen (Vorname Nachname) - das sind Kunden, keine Firmen
                if (Regex.IsMatch(name, @"^[A-Z][a-z]+\s+[A-Z][a-z]+$"))
                {
            logger.LogInformation("[PDF Extract] Skipping '{Name}' - looks like a person name", name);
                    continue;
                }
                
                supplierInfo.Name = name.Length > 100 ? name.Substring(0, 100) : name;
            logger.LogInformation("[PDF Extract] Found supplier name: {SupplierName}", supplierInfo.Name);
                break;
            }
        }

        // 3) Fallback: Name nach FATTURA-Header
        if (string.IsNullOrWhiteSpace(supplierInfo.Name))
        {
            var namePattern = @"FATTURA\s+(?:Numero fattura:.*?\s+Data:.*?\s+Fornitore\s+)?([\w\s]+(?:SRL|SpA|GmbH|AG|Inc|LLC|Ltd))";
            var nameMatch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (nameMatch.Success)
            {
                var name = CleanSupplierName(nameMatch.Groups[1].Value.Trim());
                if (!string.IsNullOrWhiteSpace(name))
                {
                    supplierInfo.Name = name.Length > 100 ? name.Substring(0, 100) : name;
                }
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
            var address = $"Via {addressMatch.Groups[1].Value.Trim()} {addressMatch.Groups[2].Value}";
            supplierInfo.Address = address.Length > 100 ? address.Substring(0, 100) : address;
        }

        // Suche nach PLZ und Stadt (italienisches Format: 00100 Roma)
        var cityPattern = @"(\d{5})\s+([A-Za-z������]+(?:\s+\([A-Z]{2}\))?)";
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

    private string CleanSupplierName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // Entferne Zeilenumbr�che und doppelte Separatoren
        var cleaned = raw.Replace("\r", " ").Replace("\n", " ");
        cleaned = string.Join(" ", cleaned.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
        cleaned = Regex.Replace(cleaned, "\\s{2,}", " ").Trim();

        // Filtere bekannte Header wie "FATTURA"
        if (Regex.IsMatch(cleaned, @"^FATTURA\b", RegexOptions.IgnoreCase)) return string.Empty;

        // Schneide alles ab, was nach einer PLZ aussieht (vermeidet dass Adresse Teil des Namens wird)
        cleaned = Regex.Replace(cleaned, @"\b\d{4,5}\b.*", "").Trim();

        // Entferne trailing L�nder-/Orts-K�rzel in Klammern
        cleaned = Regex.Replace(cleaned, @"\s*\([A-Z]{2}\)\s*$", "").Trim();

        return cleaned;
    }
}
