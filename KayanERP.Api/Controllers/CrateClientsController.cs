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
    [Authorize]
    public class CrateClientsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CrateClientsController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // ==========================================================
        // 1. عرض جميع العملاء (يتم فلترتهم تلقائيًا بواسطة Global Query Filter)
        // ==========================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CrateClient>>> GetCrateClients()
        {
            return await _context.CrateClients.ToListAsync();
        }

        // ==========================================================
        // 2. عرض عميل واحد
        // ==========================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<CrateClient>> GetCrateClient(long id)
        {
            var crateClient = await _context.CrateClients.FindAsync(id);

            if (crateClient == null)
            {
                return NotFound(new { message = "العميل غير موجود." });
            }

            return crateClient;
        }

        // ==========================================================
        // 3. إضافة عميل جديد
        // ⚠️ CompanyId يُجبر من الـ JWT (TenantContext)، وليس من الـ Body
        // ==========================================================
        [HttpPost]
        public async Task<ActionResult<CrateClient>> PostCrateClient([FromBody] CrateClient crateClient)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(crateClient.Name))
                return BadRequest(new { message = "اسم العميل مطلوب." });

            // ⚠️ فرض CompanyId من الـ JWT — نتجاهل أي قيمة من الـ Body
            crateClient.CompanyId = _tenantContext.CompanyId;

            _context.CrateClients.Add(crateClient);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCrateClient), new { id = crateClient.Id }, crateClient);
        }

        // ==========================================================
        // 4. تعديل عميل
        // ==========================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> PutCrateClient(long id, [FromBody] CrateClient crateClient)
        {
            if (id != crateClient.Id)
                return BadRequest(new { message = "رقم العميل غير متطابق." });

            // ⚠️ نقرأ العميل الأصلي للتأكد إنه موجود في نفس الشركة
            var existingClient = await _context.CrateClients.FindAsync(id);
            if (existingClient == null)
                return NotFound(new { message = "العميل غير موجود." });

            // ⚠️ نحدّث الحقول المسموح بها فقط — نتجاهل CompanyId
            existingClient.Name = crateClient.Name;
            existingClient.CrateBalance = crateClient.CrateBalance;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==========================================================
        // 5. حذف عميل
        // ==========================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteCrateClient(long id)
        {
            var crateClient = await _context.CrateClients.FindAsync(id);
            if (crateClient == null)
            {
                return NotFound(new { message = "العميل غير موجود." });
            }

            _context.CrateClients.Remove(crateClient);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}