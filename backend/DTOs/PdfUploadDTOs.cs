namespace RechnungsfreigabeAPI.DTOs;

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
