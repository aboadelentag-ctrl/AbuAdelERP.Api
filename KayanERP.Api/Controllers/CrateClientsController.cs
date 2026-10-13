using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrateClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // ربط الـ Controller بقاعدة البيانات
        public CrateClientsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. عرض جميع العملاء وأرصدتهم
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CrateClient>>> GetCrateClients()
        {
            return await _context.CrateClients.ToListAsync();
        }

        // 2. عرض عميل واحد محدد برقم الـ ID
        [HttpGet("{id}")]
        public async Task<ActionResult<CrateClient>> GetCrateClient(int id)
        {
            var crateClient = await _context.CrateClients.FindAsync(id);

            if (crateClient == null)
            {
                return NotFound();
            }

            return crateClient;
        }

        // 3. إضافة عميل جديد برصيد إسبات
        [HttpPost]
        public async Task<ActionResult<CrateClient>> PostCrateClient(CrateClient crateClient)
        {
            _context.CrateClients.Add(crateClient);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCrateClient), new { id = crateClient.Id }, crateClient);
        }

        // 4. تعديل بيانات أو رصيد عميل موجود
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCrateClient(int id, CrateClient crateClient)
        {
            if (id != crateClient.Id)
            {
                return BadRequest("رقم العميل غير متطابق.");
            }

            _context.Entry(crateClient).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CrateClientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // 5. حذف عميل نهائياً
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCrateClient(int id)
        {
            var crateClient = await _context.CrateClients.FindAsync(id);
            if (crateClient == null)
            {
                return NotFound();
            }

            _context.CrateClients.Remove(crateClient);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // دالة مساعدة للتأكد من وجود العميل
        private bool CrateClientExists(int id)
        {
            return _context.CrateClients.Any(e => e.Id == id);
        }
    }
}