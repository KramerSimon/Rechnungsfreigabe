using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Data;
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


namespace RechnungsfreigabeAPI.Services.Interfaces;

public interface IPdfUploadService
{
    Task<InvoiceDto> UploadInvoicePdfAsync(IFormFile file, int? supplierId, string? purchaseOrderId, string? costCenterId, string? projectId, int userId);
    Task<PurchaseOrderDto> UploadPurchaseOrderPdfAsync(IFormFile file, int? supplierId, string? costCenterId, string? projectId, int userId);
    Task<byte[]> GetInvoicePdfAsync(int invoiceId);
    Task<bool> DeleteInvoicePdfAsync(int invoiceId);
    Task<PdfUploadStatusDto> GetUploadStatusAsync();
}
