using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
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

        // 1. استعراض كافة العملاء
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _context.Customers
                .AsNoTracking()
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

        // 2. إضافة عميل أو منفذ بيع جديد
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                return BadRequest("اسم العميل مطلوب.");

            // توليد كود العميل تلقائياً إن لم يُدخل
            if (string.IsNullOrWhiteSpace(dto.CustomerCode))
            {
                var count = await _context.Customers.CountAsync();
                dto.CustomerCode = $"CUST-{(count + 1):D4}";
            }
            else
            {
                var exists = await _context.Customers.AnyAsync(c => c.CustomerCode == dto.CustomerCode);
                if (exists)
                    return BadRequest($"كود العميل [{dto.CustomerCode}] مسجل مسبقاً.");
            }

            var customer = new Customer
            {
                CompanyId = dto.CompanyId > 0 ? dto.CompanyId : 1,
                CustomerCode = dto.CustomerCode.Trim(),
                CustomerName = dto.CustomerName.Trim(),
                CustomerType = string.IsNullOrWhiteSpace(dto.CustomerType) ? "RETAIL" : dto.CustomerType,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                CreditLimit = dto.CreditLimit,
                PaymentTerms = dto.PaymentTerms,
                TaxNumber = dto.TaxNumber,
                Active = dto.Active,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = customer.Id, message = "تم تسجيل العميل بنجاح." });
        }

        // 3. تعديل بيانات العميل
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CustomerItemDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound("العميل غير موجود.");

            customer.CustomerName = dto.CustomerName.Trim();
            customer.CustomerType = dto.CustomerType;
            customer.Phone = dto.Phone;
            customer.Email = dto.Email;
            customer.Address = dto.Address;
            customer.CreditLimit = dto.CreditLimit;
            customer.PaymentTerms = dto.PaymentTerms;
            customer.TaxNumber = dto.TaxNumber;
            customer.Active = dto.Active;
            customer.Notes = dto.Notes;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "تم تحديث بيانات العميل بنجاح." });
        }

        // 4. تغيير حالة التفعيل (إيقاف / تنشيط)
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound("العميل غير موجود.");

            customer.Active = !customer.Active;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, active = customer.Active, message = customer.Active ? "تم تفعيل العميل." : "تم إيقاف التعامل مع العميل." });
        }
    }

    public class CustomerItemDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; } = 1;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = "RETAIL"; // RETAIL, WHOLESALE, DISTRIBUTOR
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