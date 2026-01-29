using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork unitOfWork;
    public SupplierService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
        }

    public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
    {
        var suppliers = await unitOfWork.Suppliers.GetAllAsync();
        return suppliers.OrderBy(s => s.Name).Select(MapToDto);
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        var supplier = await unitOfWork.Suppliers.GetByIdAsync(id);
        return supplier != null ? MapToDto(supplier) : null;
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto createSupplierDto)
    {
        try
        {
            var supplier = new Supplier
            {
                Name = createSupplierDto.Name,
                LegalName = createSupplierDto.LegalName,
                TaxNumber = createSupplierDto.TaxNumber,
                VatNumber = createSupplierDto.VatNumber,
                AddressLine1 = createSupplierDto.AddressLine1,
                AddressLine2 = createSupplierDto.AddressLine2,
                PostalCode = createSupplierDto.PostalCode,
                City = createSupplierDto.City,
                Country = createSupplierDto.Country,
                Email = createSupplierDto.Email,
                Phone = createSupplierDto.Phone,
                BankName = createSupplierDto.BankName,
                Iban = createSupplierDto.Iban,
                Bic = createSupplierDto.Bic,
                PaymentTermsDays = createSupplierDto.PaymentTermsDays,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            unitOfWork.Suppliers.Add(supplier);
            await unitOfWork.SaveChangesAsync();

            return MapToDto(supplier);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(int id, CreateSupplierDto updateSupplierDto)
    {
        try
        {
            var supplier = await unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier == null) return null;

            supplier.Name = updateSupplierDto.Name;
            supplier.LegalName = updateSupplierDto.LegalName;
            supplier.TaxNumber = updateSupplierDto.TaxNumber;
            supplier.VatNumber = updateSupplierDto.VatNumber;
            supplier.AddressLine1 = updateSupplierDto.AddressLine1;
            supplier.AddressLine2 = updateSupplierDto.AddressLine2;
            supplier.PostalCode = updateSupplierDto.PostalCode;
            supplier.City = updateSupplierDto.City;
            supplier.Country = updateSupplierDto.Country;
            supplier.Email = updateSupplierDto.Email;
            supplier.Phone = updateSupplierDto.Phone;
            supplier.BankName = updateSupplierDto.BankName;
            supplier.Iban = updateSupplierDto.Iban;
            supplier.Bic = updateSupplierDto.Bic;
            supplier.PaymentTermsDays = updateSupplierDto.PaymentTermsDays;
            supplier.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();

            return MapToDto(supplier);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        try
        {
            var supplier = await unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier == null) return false;

            // Hard delete - completely remove the supplier
            unitOfWork.Suppliers.Remove(supplier);
            await unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<PagedResult<SupplierDto>> GetSuppliersPagedAsync(PageRequest pageRequest)
    {
        var allSuppliers = await unitOfWork.Suppliers.GetAllAsync();
        var query = allSuppliers.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(pageRequest.SearchTerm))
        {
            var searchTerm = pageRequest.SearchTerm.ToLower();
            query = query.Where(s => 
                s.Name.ToLower().Contains(searchTerm) ||
                (s.LegalName != null && s.LegalName.ToLower().Contains(searchTerm)) ||
                (s.Email != null && s.Email.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        query = pageRequest.SortBy?.ToLower() switch
        {
            "name" => pageRequest.SortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "legalname" => pageRequest.SortDescending ? query.OrderByDescending(s => s.LegalName) : query.OrderBy(s => s.LegalName),
            "city" => pageRequest.SortDescending ? query.OrderByDescending(s => s.City) : query.OrderBy(s => s.City),
            "createdat" => pageRequest.SortDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Name)
        };

        var totalCount = await query.CountAsync();
        
        var suppliers = await query
            .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync();

        return new PagedResult<SupplierDto>
        {
            Items = suppliers.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageRequest.PageNumber,
            PageSize = pageRequest.PageSize
        };
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            LegalName = supplier.LegalName,
            TaxNumber = supplier.TaxNumber,
            VatNumber = supplier.VatNumber,
            AddressLine1 = supplier.AddressLine1,
            AddressLine2 = supplier.AddressLine2,
            PostalCode = supplier.PostalCode,
            City = supplier.City,
            Country = supplier.Country,
            Email = supplier.Email,
            Phone = supplier.Phone,
            BankName = supplier.BankName,
            Iban = supplier.Iban,
            Bic = supplier.Bic,
            PaymentTermsDays = supplier.PaymentTermsDays,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }
}