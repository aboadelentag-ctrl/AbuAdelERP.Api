using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicFormsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DynamicFormsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DynamicForm>>> GetDynamicForms()
        {
            return await _context.DynamicForms.OrderByDescending(f => f.Id).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DynamicForm>> GetDynamicForm(long id)
        {
            var form = await _context.DynamicForms.FindAsync(id);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود" });

            return form;
        }

        [HttpGet("code/{formCode}")]
        public async Task<ActionResult<DynamicForm>> GetFormByCode(string formCode)
        {
            var form = await _context.DynamicForms.FirstOrDefaultAsync(f => f.FormCode == formCode && f.IsActive == true);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود أو غير مفعل" });

            return form;
        }

        [HttpPost]
        public async Task<ActionResult<DynamicForm>> PostDynamicForm(DynamicForm dynamicForm)
        {
            if (await _context.DynamicForms.AnyAsync(f => f.FormCode == dynamicForm.FormCode))
            {
                return BadRequest(new { Message = "كود النموذج مسجل مسبقاً، يرجى اختيار كود آخر." });
            }

            // التعديل الجذري للتوافق التام مع PostgreSQL (يجب أن يكون UTC)
            dynamicForm.CreatedAt = DateTime.UtcNow;

            if (dynamicForm.IsActive == null)
            {
                dynamicForm.IsActive = true;
            }

            _context.DynamicForms.Add(dynamicForm);
            await _context.SaveChangesAsync();

            return Ok(dynamicForm);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutDynamicForm(long id, DynamicForm dynamicForm)
        {
            if (id != dynamicForm.Id) return BadRequest(new { Message = "معرف النموذج غير متطابق." });

            // تأمين توافق التواريخ دائماً مع PostgreSQL في حالة التعديل
            if (dynamicForm.CreatedAt.HasValue)
            {
                dynamicForm.CreatedAt = dynamicForm.CreatedAt.Value.ToUniversalTime();
            }

            _context.Entry(dynamicForm).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DynamicFormExists(id)) return NotFound(new { Message = "النموذج غير موجود" });
                else throw;
            }

            return NoContent();
        }

        [HttpPatch("toggle-active/{id}")]
        public async Task<IActionResult> ToggleActive(long id)
        {
            var form = await _context.DynamicForms.FindAsync(id);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود" });

            form.IsActive = !form.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تغيير حالة النموذج بنجاح", IsActive = form.IsActive });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDynamicForm(long id)
        {
            var form = await _context.DynamicForms.FindAsync(id);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود" });

            _context.DynamicForms.Remove(form);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DynamicFormExists(long id)
        {
            return _context.DynamicForms.Any(e => e.Id == id);
        }
    }
}