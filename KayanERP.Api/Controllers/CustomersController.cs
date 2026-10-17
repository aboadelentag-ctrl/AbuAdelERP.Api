using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.DTOs;
using KayanERP.Api.Models;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CustomersController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // ==============================================================
        // 1. عرض كافة العملاء مع الفلترة
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<CustomerItemDto>>> GetAll(
            [FromQuery] string? customerType,
            [FromQuery] bool? activeOnly)
        {
            var query = _context.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerType))
                query = query.Where(c => c.CustomerType == customerType);

            if (activeOnly.HasValue && activeOnly.Value)
                query = query.Where(c => c.Active);

            var customers = await query
                .OrderByDescending(c => c.Id)
                .Select(c => new CustomerItemDto
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    CustomerType = c.CustomerType ?? "RETAIL",
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    CreditLimit = c.CreditLimit,
                    PaymentTerms = c.PaymentTerms,
                    TaxNumber = c.TaxNumber,
                    Active = c.Active,
                    Notes = c.Notes
                })
                .ToListAsync();

            return Ok(customers);
        }

        // ==============================================================
        // 2. عرض عميل محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<CustomerItemDto>> GetById(long id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CustomerItemDto
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    CustomerType = c.CustomerType ?? "RETAIL",
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    CreditLimit = c.CreditLimit,
                    PaymentTerms = c.PaymentTerms,
                    TaxNumber = c.TaxNumber,
                    Active = c.Active,
                    Notes = c.Notes
                })
                .FirstOrDefaultAsync();

            if (customer == null)
                return NotFound(new { message = "العميل غير موجود." });

            return Ok(customer);
        }

        // ==============================================================
        // 3. إضافة عميل جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.CustomerName))
                return BadRequest(new { message = "اسم العميل أو التاجر مطلوب." });

            var companyId = _tenantContext.CompanyId;

            // توليد أو التحقق من الكود
            string customerCode;
            if (string.IsNullOrWhiteSpace(dto.CustomerCode))
            {
                string prefix = dto.CustomerType switch
                {
                    "DIST_DAILY" => "TRD-D",
                    "DIST_VIP" => "TRD-V",
                    "WHOLESALE" => "WHL",
                    _ => "CUST"
                };

                customerCode = $"{prefix}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            }
            else
            {
                customerCode = dto.CustomerCode.Trim();

                var exists = await _context.Customers
                    .AnyAsync(c => c.CompanyId == companyId &&
                                   c.CustomerCode.ToLower() == customerCode.ToLower());

                if (exists)
                    return Conflict(new { message = $"كود العميل [{customerCode}] مسجل مسبقاً." });
            }

            var customer = new Customer
            {
                CompanyId = companyId,
                CustomerCode = customerCode,
                CustomerName = dto.CustomerName.Trim(),
                CustomerType = string.IsNullOrWhiteSpace(dto.CustomerType) ? "RETAIL" : dto.CustomerType,
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                CreditLimit = dto.CreditLimit >= 0 ? dto.CreditLimit : 0,
                PaymentTerms = dto.PaymentTerms?.Trim(),
                TaxNumber = dto.TaxNumber?.Trim(),
                Active = dto.Active,
                Notes = dto.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new
            {
                success = true,
                id = customer.Id,
                message = "تم تسجيل العميل / التاجر بنجاح."
            });
        }

        // ==============================================================
        // 4. تعديل بيانات العميل
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = "العميل غير موجود." });

            // تأكيد ملكية العميل للشركة
            if (customer.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            // التحقق من الكود (لو تم تعديله)
            if (!string.IsNullOrWhiteSpace(dto.CustomerCode))
            {
                var trimmedCode = dto.CustomerCode.Trim();
                var codeExists = await _context.Customers
                    .AnyAsync(c => c.Id != id &&
                                   c.CompanyId == customer.CompanyId &&
                                   c.CustomerCode.ToLower() == trimmedCode.ToLower());

                if (codeExists)
                    return Conflict(new { message = $"كود العميل [{trimmedCode}] مسجل مسبقاً لعميل آخر." });

                customer.CustomerCode = trimmedCode;
            }

            // نحدّث الحقول المسموح بها
            customer.CustomerName = dto.CustomerName?.Trim() ?? customer.CustomerName;
            customer.CustomerType = string.IsNullOrWhiteSpace(dto.CustomerType) ? customer.CustomerType : dto.CustomerType;
            customer.Phone = dto.Phone?.Trim();
            customer.Email = dto.Email?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.CreditLimit = dto.CreditLimit >= 0 ? dto.CreditLimit : customer.CreditLimit;
            customer.PaymentTerms = dto.PaymentTerms?.Trim();
            customer.TaxNumber = dto.TaxNumber?.Trim();
            customer.Active = dto.Active;
            customer.Notes = dto.Notes?.Trim();
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==============================================================
        // 5. تبديل حالة التفعيل
        // ==============================================================
        [HttpPatch("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = "العميل غير موجود." });

            if (customer.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            customer.Active = !customer.Active;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                active = customer.Active,
                message = customer.Active
                    ? "تم تفعيل حساب العميل بنجاح."
                    : "تم إيقاف التعامل مع العميل مؤقتاً."
            });
        }

        // ==============================================================
        // 6. حذف العميل (Soft Delete دايمًا)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = "العميل غير موجود." });

            if (customer.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            bool hasInvoices = await _context.SalesInvoices.AnyAsync(i => i.CustomerId == id);
            bool hasCrateBalance = await _context.AssetBalances
                .AnyAsync(b => b.PartyType == "CUSTOMER" && b.PartyId == id && b.Quantity != 0);

            // Soft Delete دايمًا — أأمن من Hard Delete
            customer.Active = false;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var message = (hasInvoices || hasCrateBalance)
                ? "تم تعطيل حساب العميل بدلاً من الحذف لوجود فواتير مبيعات سابقة أو أرصدة عهدة حاويات وأسبات مرتبطة به."
                : "تم تعطيل العميل بنجاح.";

            return Ok(new { success = true, message });
        }
    }
}