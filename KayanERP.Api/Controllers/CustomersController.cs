using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. استعراض كافة العملاء ومنافذ التوزيع مع الفلترة
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? customerType, [FromQuery] bool? activeOnly)
        {
            try
            {
                var query = _context.Customers.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(customerType))
                {
                    query = query.Where(c => c.CustomerType == customerType);
                }

                if (activeOnly.HasValue && activeOnly.Value)
                {
                    query = query.Where(c => c.Active);
                }

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
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب العملاء: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. استعراض بيانات عميل محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
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

                if (customer == null) return NotFound("العميل غير موجود.");
                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استعلام العميل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. إضافة عميل أو تاجر جديد
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerItemDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                return BadRequest("اسم العميل أو التاجر مطلوب.");

            try
            {
                if (string.IsNullOrWhiteSpace(dto.CustomerCode))
                {
                    var count = await _context.Customers.CountAsync();
                    string prefix = dto.CustomerType switch
                    {
                        "DIST_DAILY" => "TRD-D",
                        "DIST_VIP" => "TRD-V",
                        "WHOLESALE" => "WHL",
                        _ => "CUST"
                    };

                    string candidateCode = $"{prefix}-{(count + 1):D4}";
                    while (await _context.Customers.AnyAsync(c => c.CustomerCode.ToLower() == candidateCode.ToLower()))
                    {
                        candidateCode = $"{prefix}-{new Random().Next(1000, 9999)}";
                    }
                    dto.CustomerCode = candidateCode;
                }
                else
                {
                    var trimmedCode = dto.CustomerCode.Trim();
                    var exists = await _context.Customers.AnyAsync(c => c.CustomerCode.ToLower() == trimmedCode.ToLower());
                    if (exists)
                        return BadRequest($"كود العميل [{trimmedCode}] مسجل مسبقاً.");
                    dto.CustomerCode = trimmedCode;
                }

                var company = await _context.Companies.FirstOrDefaultAsync();
                long companyId = dto.CompanyId > 0 ? dto.CompanyId : (company?.Id ?? 1);

                var customer = new Customer
                {
                    CompanyId = companyId,
                    CustomerCode = dto.CustomerCode,
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

                return Ok(new { success = true, id = customer.Id, message = "تم تسجيل العميل / التاجر بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ العميل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. تعديل بيانات العميل / التاجر
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CustomerItemDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                    return NotFound("العميل غير موجود.");

                if (!string.IsNullOrWhiteSpace(dto.CustomerCode))
                {
                    var trimmedCode = dto.CustomerCode.Trim();
                    var codeExists = await _context.Customers.AnyAsync(c => c.Id != id && c.CustomerCode.ToLower() == trimmedCode.ToLower());
                    if (codeExists)
                        return BadRequest($"كود العميل [{trimmedCode}] مسجل مسبقاً لعميل آخر.");
                    customer.CustomerCode = trimmedCode;
                }

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
                return Ok(new { success = true, message = "تم تحديث بيانات العميل بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تحديث بيانات العميل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. تبديل حالة التفعيل (نشط / موقوف التعامل)
        // ==============================================================
        [HttpPatch("{id:long}/toggle-status")]
        [HttpPut("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                    return NotFound("العميل غير موجود.");

                customer.Active = !customer.Active;
                customer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    active = customer.Active,
                    message = customer.Active ? "تم تفعيل حساب العميل بنجاح." : "تم إيقاف التعامل مع العميل مؤقتاً."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تغيير حالة العميل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 6. حذف العميل (محصن ضد انهيار القيود المالية وحركة الأسبات والصواني)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null) return NotFound("العميل غير موجود.");

                bool hasInvoices = await _context.SalesInvoices.AnyAsync(i => i.CustomerId == id);
                bool hasCrateBalance = await _context.AssetBalances.AnyAsync(b => b.PartyType == "CUSTOMER" && b.PartyId == id && b.Quantity != 0);

                if (hasInvoices || hasCrateBalance)
                {
                    customer.Active = false;
                    customer.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "تم تعطيل حساب العميل بدلاً من الحذف لوجود فواتير مبيعات سابقة أو أرصدة عهدة حاويات وأسبات مرتبطة به."
                    });
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "تم حذف العميل نهائياً بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حذف العميل: {ex.GetBaseException().Message}");
            }
        }
    }

    public class CustomerItemDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; } = 1;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = "RETAIL";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public decimal CreditLimit { get; set; }
        public string? PaymentTerms { get; set; }
        public string? TaxNumber { get; set; }
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
    }
}