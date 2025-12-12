using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public SeedController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("sample-data")]
    [HttpGet("sample-data")]
    public async Task<IActionResult> SeedSampleData()
    {
        try
        {
            // Clear existing data to refresh (in correct order for foreign keys)
            _context.ApprovalWorkflows.RemoveRange(_context.ApprovalWorkflows);
            _context.Notifications.RemoveRange(_context.Notifications);
            _context.Invoices.RemoveRange(_context.Invoices);
            _context.Suppliers.RemoveRange(_context.Suppliers);
            await _context.SaveChangesAsync();

            // Seed Suppliers first
            var suppliers = new List<Supplier>
            {
                new Supplier { Id = 1, Name = "Microsoft Deutschland", LegalName = "Microsoft Deutschland GmbH", Email = "buchhaltung@microsoft.de", City = "München", Country = "Deutschland" },
                new Supplier { Id = 2, Name = "Amazon Business", LegalName = "Amazon EU S.à r.l.", Email = "business@amazon.de", City = "Berlin", Country = "Deutschland" },
                new Supplier { Id = 3, Name = "Büroservice Express", LegalName = "Büroservice Express GmbH", Email = "info@bueroservice.de", City = "Hamburg", Country = "Deutschland" },
                new Supplier { Id = 4, Name = "IT-Solutions", LegalName = "IT-Solutions & Development GmbH", Email = "contact@it-solutions.de", City = "Frankfurt", Country = "Deutschland" },
                new Supplier { Id = 5, Name = "Office World", LegalName = "Office World Handels-GmbH", Email = "service@officeworld.de", City = "Köln", Country = "Deutschland" }
            };

            await _context.Suppliers.AddRangeAsync(suppliers);
            await _context.SaveChangesAsync();

            // Seed Invoices
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    Id = 1,
                    InvoiceNumber = "MS-2024-001",
                    SupplierId = 1,
                    CostCenterId = "IT",
                    ProjectId = "WEB001",
                    NetAmount = 840.34m,
                    TaxAmount = 159.66m,
                    TotalAmount = 1000.00m,
                    InvoiceDate = new DateTime(2024, 11, 15),
                    DueDate = new DateTime(2024, 12, 15),
                    Description = "Microsoft Office 365 Lizenzen für 12 Monate",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 2,
                    InvoiceNumber = "AMZ-2024-078",
                    SupplierId = 2,
                    CostCenterId = "OFFICE",
                    ProjectId = "OFF004",
                    NetAmount = 42.02m,
                    TaxAmount = 7.98m,
                    TotalAmount = 50.00m,
                    InvoiceDate = new DateTime(2024, 11, 20),
                    DueDate = new DateTime(2024, 12, 4),
                    Description = "Büromaterial: Druckerpapier, Stifte, Ordner",
                    Status = InvoiceStatus.Eingegangen,
                    CreatedBy = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 3,
                    InvoiceNumber = "BE-2024-456",
                    SupplierId = 3,
                    CostCenterId = "OFFICE",
                    NetAmount = 25.21m,
                    TaxAmount = 4.79m,
                    TotalAmount = 30.00m,
                    InvoiceDate = new DateTime(2024, 11, 18),
                    DueDate = new DateTime(2024, 12, 18),
                    Description = "Reinigungsmittel für Büroküche",
                    Status = InvoiceStatus.Freigegeben,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 4,
                    InvoiceNumber = "ITS-2024-789",
                    SupplierId = 4,
                    CostCenterId = "IT",
                    ProjectId = "WEB001",
                    NetAmount = 4201.68m,
                    TaxAmount = 798.32m,
                    TotalAmount = 5000.00m,
                    InvoiceDate = new DateTime(2024, 11, 10),
                    DueDate = new DateTime(2024, 12, 10),
                    Description = "Webentwicklung und Design Services",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 5,
                    InvoiceNumber = "OW-2024-123",
                    SupplierId = 5,
                    CostCenterId = "HR",
                    ProjectId = "HR002",
                    NetAmount = 168.07m,
                    TaxAmount = 31.93m,
                    TotalAmount = 200.00m,
                    InvoiceDate = new DateTime(2024, 11, 22),
                    DueDate = new DateTime(2024, 12, 13),
                    Description = "Bürostühle für neue Mitarbeiter",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 6,
                    InvoiceNumber = "HIGH-2024-001",
                    SupplierId = 1,
                    CostCenterId = "IT",
                    NetAmount = 8403.36m,
                    TaxAmount = 1596.64m,
                    TotalAmount = 10000.00m,
                    InvoiceDate = new DateTime(2024, 12, 1),
                    DueDate = new DateTime(2024, 12, 31),
                    Description = "Enterprise Software Lizenzen - Jahresvertrag",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 7,
                    InvoiceNumber = "URGENT-2024-789",
                    SupplierId = 4,
                    CostCenterId = "IT", 
                    ProjectId = "WEB001",
                    NetAmount = 12605.04m,
                    TaxAmount = 2394.96m,
                    TotalAmount = 15000.00m,
                    InvoiceDate = new DateTime(2024, 12, 5),
                    DueDate = new DateTime(2024, 12, 20),
                    Description = "Kritisches Security Update & Performance Optimization",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 8,
                    InvoiceNumber = "ADMIN-2024-555",
                    SupplierId = 2,
                    CostCenterId = "OFFICE",
                    NetAmount = 2521.01m,
                    TaxAmount = 478.99m,
                    TotalAmount = 3000.00m,
                    InvoiceDate = new DateTime(2024, 12, 8),
                    DueDate = new DateTime(2024, 12, 25),
                    Description = "Premium Office Equipment & Furniture",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 3,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Invoices.AddRangeAsync(invoices);
            await _context.SaveChangesAsync();

            // Seed ApprovalWorkflows - All requiring Admin (User ID: 1) approval
            var workflows = new List<ApprovalWorkflow>
            {
                new ApprovalWorkflow
                {
                    InvoiceId = 1,
                    RuleId = 2,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval
                    ApprovalLevel = 1,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 4,
                    RuleId = 3,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval
                    ApprovalLevel = 1,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 5,
                    RuleId = 3,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval
                    ApprovalLevel = 1,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 6,
                    RuleId = 2,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval for high-value invoice
                    ApprovalLevel = 2,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 7,
                    RuleId = 1,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval for urgent invoice
                    ApprovalLevel = 2,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 8,
                    RuleId = 2,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval
                    ApprovalLevel = 1,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.ApprovalWorkflows.AddRangeAsync(workflows);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sample data seeded successfully", 
                suppliers = suppliers.Count, 
                invoices = invoices.Count,
                workflows = workflows.Count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}