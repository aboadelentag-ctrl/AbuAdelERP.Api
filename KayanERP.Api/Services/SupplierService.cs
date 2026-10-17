using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Models;
using KayanERP.Api.Services;

namespace KayanERP.Api.Services
{
    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetAllSuppliersAsync();
        Task<bool> CreateSupplierAsync(SupplierCreateDto dto);
    }

    public class SupplierService : ISupplierService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public SupplierService(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task<List<SupplierDto>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName,
                    Phone = s.Phone,
                    Email = s.Email,
                    Address = s.Address,
                    TaxNumber = s.TaxNumber,
                    PaymentTerms = s.PaymentTerms,
                    CreditLimit = s.CreditLimit,
                    Active = s.Active,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> CreateSupplierAsync(SupplierCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SupplierName))
                return false;

            var companyId = _tenantContext.CompanyId;

            var supplier = new Supplier
            {
                CompanyId = companyId,
                SupplierCode = string.IsNullOrWhiteSpace(dto.SupplierCode)
                    ? $"SUP-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
                    : dto.SupplierCode.Trim(),
                SupplierName = dto.SupplierName.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                TaxNumber = dto.TaxNumber?.Trim(),
                PaymentTerms = dto.PaymentTerms?.Trim() ?? "نقدي",
                CreditLimit = dto.CreditLimit ?? 0,
                Active = dto.Active,
                Notes = dto.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}