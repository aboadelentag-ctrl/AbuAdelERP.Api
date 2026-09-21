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
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompaniesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            return await _context.Companies.OrderByDescending(c => c.Id).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompany(long id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            return company;
        }

        [HttpPost]
        public async Task<ActionResult<Company>> PostCompany([FromBody] Company company)
        {
            company.CreatedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest($"رفضت قاعدة البيانات الحفظ: {innerMessage}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCompany(long id, [FromBody] Company company)
        {
            if (id != company.Id) return BadRequest("معرف الشركة غير متطابق.");

            var existingCompany = await _context.Companies.FindAsync(id);
            if (existingCompany == null) return NotFound("الشركة غير موجودة.");

            existingCompany.CompanyCode = company.CompanyCode;
            existingCompany.CompanyName = company.CompanyName;
            existingCompany.Phone = company.Phone;
            existingCompany.Email = company.Email;
            existingCompany.Address = company.Address;
            existingCompany.Active = company.Active;
            existingCompany.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest($"رفضت قاعدة البيانات التعديل: {innerMessage}");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(long id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            company.Active = false; // Soft Delete
            company.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}