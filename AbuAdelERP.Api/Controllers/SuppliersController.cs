using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpGet]
        public async Task<IActionResult> GetSuppliers([FromQuery] string? status = null, [FromQuery] string? approval = null)
        {
            var query = _context.Suppliers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active") query = query.Where(s => s.Active);
                else if (status == "inactive") query = query.Where(s => !s.Active);
            }

            if (!string.IsNullOrEmpty(approval))
            {
                if (approval == "approved") query = query.Where(s => s.IsApproved);
                else if (approval == "unapproved") query = query.Where(s => !s.IsApproved);
            }

            var list = await query
                .OrderByDescending(s => s.Id)
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName,
                    Phone = s.Phone,
                    Email = s.Email,
                    CreditLimit = s.CreditLimit,
                    Active = s.Active,
                    IsApproved = s.IsApproved,
                    Notes = s.Notes
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplier(long id)
        {
            var s = await _context.Suppliers.FindAsync(id);
            if (s == null) return NotFound("المورد غير موجود.");

            return Ok(new SupplierDto
            {
                Id = s.Id,
                CompanyId = s.CompanyId,
                SupplierCode = s.SupplierCode,
                SupplierName = s.SupplierName,
                Phone = s.Phone,
                Email = s.Email,
                CreditLimit = s.CreditLimit,
                Active = s.Active,
                IsApproved = s.IsApproved,
                Notes = s.Notes
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierName) || string.IsNullOrWhiteSpace(dto.SupplierCode))
            {
                return BadRequest("كود المورد واسمه مطلوبان.");
            }

            bool exists = await _context.Suppliers.AnyAsync(s => s.SupplierCode == dto.SupplierCode);
            if (exists) return BadRequest("كود المورد مسجل مسبقاً.");

            long defaultCompanyId = 1;
            var firstComp = await _context.Companies.Select(c => c.Id).FirstOrDefaultAsync();
            if (firstComp > 0) defaultCompanyId = firstComp;

            var supplier = new Supplier
            {
                CompanyId = defaultCompanyId,
                SupplierCode = dto.SupplierCode.Trim(),
                SupplierName = dto.SupplierName.Trim(),
                Phone = dto.Phone,
                Email = dto.Email,
                CreditLimit = dto.CreditLimit,
                Active = dto.Active,
                IsApproved = dto.IsApproved,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم حفظ بيانات المورد بنجاح." });
        }

        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(long id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound("المورد غير موجود.");

            supplier.Active = !supplier.Active;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تحديث حالة النشاط بنجاح.", Active = supplier.Active });
        }

        [HttpPatch("{id}/toggle-approval")]
        public async Task<IActionResult> ToggleApproval(long id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound("المورد غير موجود.");

            supplier.IsApproved = !supplier.IsApproved;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تحديث حالة اعتماد المورد بنجاح.", IsApproved = supplier.IsApproved });
        }
    }

    public class SupplierDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal CreditLimit { get; set; }
        public bool Active { get; set; }
        public bool IsApproved { get; set; }
        public string? Notes { get; set; }
    }

    public class SupplierCreateDto
    {
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal CreditLimit { get; set; } = 0;
        public bool Active { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public string? Notes { get; set; }
    }
}