using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CompaniesController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // ==========================================================
        // 1. عرض كل الشركات — SuperAdmin فقط
        // ==========================================================
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,مدير النظام التنفيذي")]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            return await _context.Companies
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        // ==========================================================
        // 2. عرض شركة واحدة — المستخدم يشوف شركته بس
        // ==========================================================
        [HttpGet("{id:long}")]
        [Authorize]
        public async Task<ActionResult<Company>> GetCompany(long id)
        {
            // ⚠️ منع الوصول لشركة تانية
            if (id != _tenantContext.CompanyId)
                return Forbid();

            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null) return NotFound();
            return company;
        }

        // ==========================================================
        // 3. إضافة شركة جديدة — مفتوح للتسجيل (يُحمى بـ Rate Limiting لاحقًا)
        // ==========================================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Company>> PostCompany([FromBody] Company company)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(company.CompanyCode) ||
                string.IsNullOrWhiteSpace(company.CompanyName))
                return BadRequest(new { message = "كود الشركة والاسم مطلوبان." });

            // تحقق من عدم تكرار الكود
            var exists = await _context.Companies
                .AnyAsync(c => c.CompanyCode == company.CompanyCode);

            if (exists)
                return Conflict(new { message = "كود الشركة مسجل بالفعل." });

            company.CreatedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;
            company.Active = true;

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
        }

        // ==========================================================
        // 4. تعديل شركة — المستخدم يعدّل شركته بس
        // ==========================================================
        [HttpPut("{id:long}")]
        [Authorize]
        public async Task<IActionResult> PutCompany(long id, [FromBody] Company company)
        {
            // ⚠️ منع تعديل شركة تانية
            if (id != _tenantContext.CompanyId)
                return Forbid();

            if (id != company.Id)
                return BadRequest(new { message = "معرف الشركة غير متطابق." });

            var existingCompany = await _context.Companies.FindAsync(id);
            if (existingCompany == null) return NotFound(new { message = "الشركة غير موجودة." });

            // ⚠️ نحدّث الحقول المسموح بها فقط — نتجاهل TenantKey و TenantCode
            existingCompany.CompanyCode = company.CompanyCode;
            existingCompany.CompanyName = company.CompanyName;
            existingCompany.Phone = company.Phone;
            existingCompany.Email = company.Email;
            existingCompany.Address = company.Address;
            existingCompany.Active = company.Active;
            existingCompany.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==========================================================
        // 5. تعطيل شركة (Soft Delete) — SuperAdmin فقط
        // ==========================================================
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "SuperAdmin,مدير النظام التنفيذي")]
        public async Task<IActionResult> DeleteCompany(long id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = "الشركة غير موجودة." });

            company.Active = false;
            company.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}