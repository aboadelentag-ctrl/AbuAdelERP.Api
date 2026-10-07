using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SuppliersController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب كافة الموردين مع الفلترة والبحث السريع
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool? active = null)
        {
            try
            {
                var query = _context.Suppliers.AsNoTracking();

                if (active.HasValue)
                {
                    query = query.Where(s => s.Active == active.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(s => s.SupplierName.ToLower().Contains(term)
                                          || s.SupplierCode.ToLower().Contains(term)
                                          || (s.Phone != null && s.Phone.Contains(term))
                                          || (s.TaxNumber != null && s.TaxNumber.Contains(term)));
                }

                var list = await query
                    .OrderBy(s => s.SupplierName)
                    .Select(s => new SupplierViewDto
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
                        Active = s.Active,
                        Notes = s.Notes,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات الموردين: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب بيانات مورد محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SupplierViewDto
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
                    Active = s.Active,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (supplier == null) return NotFound("المورد غير مسجل بالنظام.");
            return Ok(supplier);
        }

        // ==============================================================
        // 3. إضافة مورد جديد مع توليد الكود ومنع التكرار
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SupplierCreateUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierName))
                return BadRequest("اسم المورد مطلوب.");

            var company = await _context.Companies.FirstOrDefaultAsync();
            long companyId = company?.Id ?? 1;

            string code = dto.SupplierCode?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                int count = await _context.Suppliers.CountAsync(s => s.CompanyId == companyId);
                code = $"SUP-{(count + 1):D4}";
            }
            else
            {
                bool exists = await _context.Suppliers.AnyAsync(s => s.CompanyId == companyId && s.SupplierCode.ToLower() == code.ToLower());
                if (exists) return BadRequest($"كود المورد ({code}) مسجل بالفعل لمورد آخر.");
            }

            var supplier = new Supplier
            {
                CompanyId = companyId,
                SupplierCode = code,
                SupplierName = dto.SupplierName.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                TaxNumber = dto.TaxNumber?.Trim(),
                PaymentTerms = dto.PaymentTerms?.Trim() ?? "نقدي",
                Active = dto.Active,
                Notes = dto.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return Ok(new SupplierViewDto
            {
                Id = supplier.Id,
                CompanyId = supplier.CompanyId,
                SupplierCode = supplier.SupplierCode,
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                TaxNumber = supplier.TaxNumber,
                PaymentTerms = supplier.PaymentTerms,
                Active = supplier.Active,
                Notes = supplier.Notes,
                CreatedAt = supplier.CreatedAt
            });
        }

        // ==============================================================
        // 4. تعديل بيانات مورد
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] SupplierCreateUpdateDto dto)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound("المورد غير موجود.");

            if (string.IsNullOrWhiteSpace(dto.SupplierName))
                return BadRequest("اسم المورد مطلوب.");

            if (!string.IsNullOrWhiteSpace(dto.SupplierCode))
            {
                string code = dto.SupplierCode.Trim();
                bool codeExists = await _context.Suppliers
                    .AnyAsync(s => s.CompanyId == supplier.CompanyId && s.SupplierCode.ToLower() == code.ToLower() && s.Id != id);

                if (codeExists) return BadRequest($"كود المورد ({code}) مستخدم في سجل مورد آخر.");
                supplier.SupplierCode = code;
            }

            supplier.SupplierName = dto.SupplierName.Trim();
            supplier.Phone = dto.Phone?.Trim();
            supplier.Email = dto.Email?.Trim();
            supplier.Address = dto.Address?.Trim();
            supplier.TaxNumber = dto.TaxNumber?.Trim();
            supplier.PaymentTerms = dto.PaymentTerms?.Trim() ?? supplier.PaymentTerms;
            supplier.Active = dto.Active;
            supplier.Notes = dto.Notes?.Trim();
            supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==============================================================
        // 5. تبديل حالة التفعيل
        // ==============================================================
        [HttpPatch("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound("المورد غير موجود.");

            supplier.Active = !supplier.Active;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(supplier.Active);
        }
    }

    // ==============================================================
    // DTOs الموردين المتوافقة مع الكيان
    // ==============================================================
    public class SupplierViewDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? PaymentTerms { get; set; }
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class SupplierCreateUpdateDto
    {
        public string? SupplierCode { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? PaymentTerms { get; set; } = "نقدي";
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
    }
}