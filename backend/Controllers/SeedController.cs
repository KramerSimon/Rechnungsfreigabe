using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Services;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    
    public SeedController(ApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpPost("init-database")]
    [HttpGet("init-database")]
    public async Task<IActionResult> InitializeDatabase()
    {
        try
        {
            // Check if database already has users
            var existingUsers = await _context.Users.AnyAsync();
            if (existingUsers)
            {
                return Ok(new { message = "Database already initialized", userCount = await _context.Users.CountAsync() });
            }

            // Create Roles
            var roles = new List<Role>
            {
                new Role { Id = 1, Name = "Administrator", Description = "Vollzugriff auf alle Funktionen", Permissions = "[\"all\"]" },
                new Role { Id = 2, Name = "Freigeber", Description = "Kann Rechnungen freigeben", Permissions = "[\"approve_invoices\", \"view_all_invoices\"]" },
                new Role { Id = 3, Name = "Buchhaltung", Description = "Buchhaltungsfunktionen", Permissions = "[\"view_all_invoices\", \"process_payments\", \"view_reports\"]" },
                new Role { Id = 4, Name = "Sachbearbeiter", Description = "Grundlegende Rechnungserfassung", Permissions = "[\"create_invoices\", \"view_own_invoices\"]" },
                new Role { Id = 5, Name = "Controller", Description = "Kann Reports einsehen", Permissions = "[\"view_reports\", \"view_all_invoices\"]" },
                new Role { Id = 6, Name = "Manager", Description = "Kann Team-Rechnungen verwalten", Permissions = "[\"approve_cost_center_invoices\", \"view_team_invoices\"]" }
            };
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();

            // Create Users (password: Password123!)
            var passwordHash = _passwordService.HashPassword("Password123!");
            var users = new List<User>
            {
                new User { Id = 1, Username = "admin", PasswordHash = passwordHash, Email = "admin@example.com", FirstName = "System", LastName = "Administrator", IsActive = true },
                new User { Id = 2, Username = "max.mustermann", PasswordHash = passwordHash, Email = "max.mustermann@example.com", FirstName = "Max", LastName = "Mustermann", IsActive = true },
                new User { Id = 3, Username = "maria.mueller", PasswordHash = passwordHash, Email = "maria.mueller@example.com", FirstName = "Maria", LastName = "Müller", IsActive = true },
                new User { Id = 4, Username = "hans.schmidt", PasswordHash = passwordHash, Email = "hans.schmidt@example.com", FirstName = "Hans", LastName = "Schmidt", IsActive = true },
                new User { Id = 5, Username = "lisa.klein", PasswordHash = passwordHash, Email = "lisa.klein@example.com", FirstName = "Lisa", LastName = "Klein", IsActive = true }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Assign Roles to Users
            var userRoles = new List<UserRole>
            {
                new UserRole { UserId = 1, RoleId = 1 },
                new UserRole { UserId = 2, RoleId = 2 },
                new UserRole { UserId = 3, RoleId = 3 },
                new UserRole { UserId = 4, RoleId = 6 },
                new UserRole { UserId = 5, RoleId = 5 }
            };
            await _context.UserRoles.AddRangeAsync(userRoles);
            await _context.SaveChangesAsync();

            // Create Cost Centers
            var costCenters = new List<CostCenter>
            {
                new CostCenter { Id = "IT", Name = "IT & Development", Description = "IT-Abteilung und Softwareentwicklung", Budget = 500000m, IsActive = true, ManagerId = 4 },
                new CostCenter { Id = "OFFICE", Name = "Office & Administration", Description = "Büroverwaltung und allgemeine Kosten", Budget = 100000m, IsActive = true, ManagerId = 1 },
                new CostCenter { Id = "HR", Name = "Human Resources", Description = "Personalabteilung", Budget = 200000m, IsActive = true, ManagerId = 3 },
                new CostCenter { Id = "ADMIN", Name = "Administration", Description = "Verwaltung", Budget = 150000m, IsActive = true, ManagerId = 1 },
                new CostCenter { Id = "SALES", Name = "Sales & Marketing", Description = "Vertrieb und Marketing", Budget = 300000m, IsActive = true, ManagerId = 2 },
                new CostCenter { Id = "FINANCE", Name = "Finance & Controlling", Description = "Finanzabteilung", Budget = 250000m, IsActive = true, ManagerId = 5 }
            };
            await _context.CostCenters.AddRangeAsync(costCenters);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Database initialized successfully!", 
                users = users.Count,
                roles = roles.Count,
                costCenters = costCenters.Count,
                defaultPassword = "Password123!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stack = ex.StackTrace });
        }
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
            _context.InvoiceHistories.RemoveRange(_context.InvoiceHistories);
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

            // Ensure Cost Centers exist (don't delete them, just add if missing)
            var costCenterIds = new[] { "IT", "OFFICE", "HR", "ADMIN", "SALES", "FINANCE" };
            var existingCostCenters = await _context.CostCenters
                .Where(cc => costCenterIds.Contains(cc.Id))
                .Select(cc => cc.Id)
                .ToListAsync();

            var missingCostCenterIds = costCenterIds.Except(existingCostCenters).ToList();
            if (missingCostCenterIds.Any())
            {
                var newCostCenters = new List<CostCenter>();
                if (missingCostCenterIds.Contains("IT"))
                    newCostCenters.Add(new CostCenter { Id = "IT", Name = "IT & Development", Budget = 500000, IsActive = true });
                if (missingCostCenterIds.Contains("OFFICE"))
                    newCostCenters.Add(new CostCenter { Id = "OFFICE", Name = "Office & Administration", Budget = 100000, IsActive = true });
                if (missingCostCenterIds.Contains("HR"))
                    newCostCenters.Add(new CostCenter { Id = "HR", Name = "Human Resources", Budget = 200000, IsActive = true });
                if (missingCostCenterIds.Contains("ADMIN"))
                    newCostCenters.Add(new CostCenter { Id = "ADMIN", Name = "Administration", Budget = 150000, IsActive = true });
                if (missingCostCenterIds.Contains("SALES"))
                    newCostCenters.Add(new CostCenter { Id = "SALES", Name = "Sales & Marketing", Budget = 300000, IsActive = true });
                if (missingCostCenterIds.Contains("FINANCE"))
                    newCostCenters.Add(new CostCenter { Id = "FINANCE", Name = "Finance & Controlling", Budget = 250000, IsActive = true });
                
                await _context.CostCenters.AddRangeAsync(newCostCenters);
                await _context.SaveChangesAsync();
            }

            // Seed Invoices with current dates (December 15, 2025 onwards)
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    Id = 1,
                    InvoiceNumber = "MS-2025-001",
                    SupplierId = 1,
                    CostCenterId = "IT",
                    ProjectId = "WEB001",
                    NetAmount = 840.34m,
                    TaxAmount = 159.66m,
                    TotalAmount = 1000.00m,
                    InvoiceDate = new DateTime(2025, 12, 15),
                    DueDate = new DateTime(2026, 1, 15),
                    Description = "Microsoft Office 365 Lizenzen für 12 Monate",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 2,
                    InvoiceNumber = "AMZ-2025-078",
                    SupplierId = 2,
                    CostCenterId = "OFFICE",
                    ProjectId = "OFF004",
                    NetAmount = 42.02m,
                    TaxAmount = 7.98m,
                    TotalAmount = 50.00m,
                    InvoiceDate = new DateTime(2025, 12, 16),
                    DueDate = new DateTime(2025, 12, 30),
                    Description = "Büromaterial: Druckerpapier, Stifte, Ordner",
                    Status = InvoiceStatus.Eingegangen,
                    CreatedBy = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 3,
                    InvoiceNumber = "BE-2025-456",
                    SupplierId = 3,
                    CostCenterId = "OFFICE",
                    NetAmount = 25.21m,
                    TaxAmount = 4.79m,
                    TotalAmount = 30.00m,
                    InvoiceDate = new DateTime(2025, 12, 17),
                    DueDate = new DateTime(2026, 1, 16),
                    Description = "Reinigungsmittel für Büroküche",
                    Status = InvoiceStatus.Freigegeben,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 4,
                    InvoiceNumber = "ITS-2025-789",
                    SupplierId = 4,
                    CostCenterId = "IT",
                    ProjectId = "WEB001",
                    NetAmount = 4201.68m,
                    TaxAmount = 798.32m,
                    TotalAmount = 5000.00m,
                    InvoiceDate = new DateTime(2025, 12, 18),
                    DueDate = new DateTime(2026, 1, 17),
                    Description = "Webentwicklung und Design Services",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 5,
                    InvoiceNumber = "OW-2025-123",
                    SupplierId = 5,
                    CostCenterId = "HR",
                    ProjectId = "HR002",
                    NetAmount = 168.07m,
                    TaxAmount = 31.93m,
                    TotalAmount = 200.00m,
                    InvoiceDate = new DateTime(2025, 12, 19),
                    DueDate = new DateTime(2026, 1, 2),
                    Description = "Bürostühle für neue Mitarbeiter",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 6,
                    InvoiceNumber = "HIGH-2025-001",
                    SupplierId = 1,
                    CostCenterId = "IT",
                    NetAmount = 8403.36m,
                    TaxAmount = 1596.64m,
                    TotalAmount = 10000.00m,
                    InvoiceDate = new DateTime(2025, 12, 20),
                    DueDate = new DateTime(2026, 1, 19),
                    Description = "Enterprise Software Lizenzen - Jahresvertrag",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 7,
                    InvoiceNumber = "URGENT-2025-789",
                    SupplierId = 4,
                    CostCenterId = "IT", 
                    ProjectId = "WEB001",
                    NetAmount = 12605.04m,
                    TaxAmount = 2394.96m,
                    TotalAmount = 15000.00m,
                    InvoiceDate = new DateTime(2025, 12, 21),
                    DueDate = new DateTime(2026, 1, 4),
                    Description = "Kritisches Security Update & Performance Optimization",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 8,
                    InvoiceNumber = "ADMIN-2025-555",
                    SupplierId = 2,
                    CostCenterId = "OFFICE",
                    NetAmount = 2521.01m,
                    TaxAmount = 478.99m,
                    TotalAmount = 3000.00m,
                    InvoiceDate = new DateTime(2025, 12, 22),
                    DueDate = new DateTime(2026, 1, 21),
                    Description = "Premium Office Equipment & Furniture",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Invoice
                {
                    Id = 9,
                    InvoiceNumber = "TRAVEL-2025-001",
                    SupplierId = 1,
                    CostCenterId = "ADMIN",
                    ProjectId = "CONF001",
                    NetAmount = 3386.55m,
                    TaxAmount = 643.45m,
                    TotalAmount = 4030.00m,
                    InvoiceDate = new DateTime(2025, 12, 23),
                    DueDate = new DateTime(2026, 1, 22),
                    Description = "Geschäftsreise: Flugtickets für Konferenz Berlin",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new Invoice
                {
                    Id = 10,
                    InvoiceNumber = "MAINT-2025-012",
                    SupplierId = 3,
                    CostCenterId = "IT",
                    ProjectId = "INF001",
                    NetAmount = 1680.67m,
                    TaxAmount = 319.33m,
                    TotalAmount = 2000.00m,
                    InvoiceDate = new DateTime(2025, 12, 24),
                    DueDate = new DateTime(2026, 1, 23),
                    Description = "Wartung und Support: Server-Infrastruktur (Q4)",
                    Status = InvoiceStatus.Eingegangen,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow.AddHours(-12)
                },
                new Invoice
                {
                    Id = 11,
                    InvoiceNumber = "TRAINING-2025-003",
                    SupplierId = 4,
                    CostCenterId = "HR",
                    ProjectId = "DEV001",
                    NetAmount = 5042.02m,
                    TaxAmount = 957.98m,
                    TotalAmount = 6000.00m,
                    InvoiceDate = new DateTime(2025, 12, 26),
                    DueDate = new DateTime(2026, 1, 25),
                    Description = "Schulung: Advanced .NET Development für Team",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                },
                new Invoice
                {
                    Id = 12,
                    InvoiceNumber = "SOFT-2025-789",
                    SupplierId = 5,
                    CostCenterId = "IT",
                    ProjectId = "TOOL001",
                    NetAmount = 8050.42m,
                    TaxAmount = 1529.58m,
                    TotalAmount = 9580.00m,
                    InvoiceDate = new DateTime(2025, 12, 27),
                    DueDate = new DateTime(2026, 1, 26),
                    Description = "Lizenzen: Development Tools & IDE (Annual)",
                    Status = InvoiceStatus.Freigabe_Erforderlich,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
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
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 9,
                    RuleId = 3,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval for travel expenses
                    ApprovalLevel = 1,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 10,
                    RuleId = 2,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval for maintenance
                    ApprovalLevel = 1,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 11,
                    RuleId = 2,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval for training
                    ApprovalLevel = 1,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new ApprovalWorkflow
                {
                    InvoiceId = 12,
                    RuleId = 2,
                    StepNumber = 1,
                    ApproverId = 1, // Admin approval for software licenses
                    ApprovalLevel = 2,
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
