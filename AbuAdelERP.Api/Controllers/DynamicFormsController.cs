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

        // 1. استعراض كافة النماذج
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DynamicForm>>> GetDynamicForms()
        {
            return await _context.DynamicForms.OrderByDescending(f => f.Id).ToListAsync();
        }

        // 2. جلب نموذج محدد بالـ Id
        [HttpGet("{id}")]
        public async Task<ActionResult<DynamicForm>> GetDynamicForm(long id)
        {
            var form = await _context.DynamicForms.FindAsync(id);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود" });

            return form;
        }

        // 3. جلب نموذج محدد بكود الربط
        [HttpGet("code/{formCode}")]
        public async Task<ActionResult<DynamicForm>> GetFormByCode(string formCode)
        {
            var form = await _context.DynamicForms.FirstOrDefaultAsync(f => f.FormCode == formCode && f.IsActive == true);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود أو غير مفعل" });

            return form;
        }

        // 4. إنشاء نموذج جديد
        [HttpPost]
        public async Task<ActionResult<DynamicForm>> PostDynamicForm(DynamicForm dynamicForm)
        {
            if (await _context.DynamicForms.AnyAsync(f => f.FormCode == dynamicForm.FormCode))
            {
                return BadRequest(new { Message = "كود النموذج مسجل مسبقاً، يرجى اختيار كود آخر." });
            }

            dynamicForm.CreatedAt = DateTime.UtcNow;

            if (dynamicForm.IsActive == null)
            {
                dynamicForm.IsActive = true;
            }

            _context.DynamicForms.Add(dynamicForm);
            await _context.SaveChangesAsync();

            return Ok(dynamicForm);
        }

        // 5. تعديل تصميم النموذج
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDynamicForm(long id, DynamicForm dynamicForm)
        {
            if (id != dynamicForm.Id) return BadRequest(new { Message = "معرف النموذج غير متطابق." });

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

        // 6. تفعيل / إيقاف النموذج
        [HttpPatch("toggle-active/{id}")]
        public async Task<IActionResult> ToggleActive(long id)
        {
            var form = await _context.DynamicForms.FindAsync(id);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود" });

            form.IsActive = !form.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تغيير حالة النموذج بنجاح", IsActive = form.IsActive });
        }

        // 7. حذف النموذج
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDynamicForm(long id)
        {
            var form = await _context.DynamicForms.FindAsync(id);
            if (form == null) return NotFound(new { Message = "النموذج غير موجود" });

            _context.DynamicForms.Remove(form);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ==============================================================
        // 8. حفظ الاستجابات ومحاضر التفتيش الميدانية
        // ==============================================================
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitFormResponse([FromBody] FormSubmissionDto dto)
        {
            if (dto == null || dto.FormId <= 0)
                return BadRequest(new { Message = "بيانات الاستمارة غير صالحة." });

            var form = await _context.DynamicForms.FindAsync(dto.FormId);
            if (form == null)
                return NotFound(new { Message = "النموذج الأصلي غير موجود." });

            try
            {
                var submission = new FormSubmission
                {
                    FormId = dto.FormId,
                    SubmissionData = dto.SubmissionData,
                    UserId = dto.UserId > 0 ? dto.UserId : 1,
                    Status = !string.IsNullOrWhiteSpace(dto.Status) ? dto.Status : "مكتمل"
                };

                _context.FormSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                return Ok(new { Success = true, Message = "تم حفظ واعتماد محضر التفتيش بنجاح.", SubmissionId = submission.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"خطأ أثناء حفظ استمارة التفتيش: {ex.GetBaseException().Message}" });
            }
        }

        // ==============================================================
        // 9. استرجاع استجابات الفحص السابقة للتقارير
        // ==============================================================
        [HttpGet("submissions/{formId:long}")]
        public async Task<IActionResult> GetSubmissionsByForm(long formId)
        {
            try
            {
                var submissions = await _context.FormSubmissions
                    .AsNoTracking()
                    .Where(s => s.FormId == formId)
                    .OrderByDescending(s => s.Id)
                    .ToListAsync();

                return Ok(submissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"خطأ في جلب السجلات: {ex.Message}" });
            }
        }

        private bool DynamicFormExists(long id)
        {
            return _context.DynamicForms.Any(e => e.Id == id);
        }
    }

    public class FormSubmissionDto
    {
        public long FormId { get; set; }
        public string SubmissionData { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string? SubmittedByName { get; set; }
        public string Status { get; set; } = "مكتمل";
    }
}