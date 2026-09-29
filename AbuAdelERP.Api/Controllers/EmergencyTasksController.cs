using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmergencyTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmergencyTasksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/EmergencyTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmergencyTask>>> GetEmergencyTasks()
        {
            try
            {
                return await _context.EmergencyTasks.OrderByDescending(t => t.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استرجاع سجل الطوارئ: {ex.Message}");
            }
        }

        // POST: api/EmergencyTasks
        [HttpPost]
        public async Task<ActionResult<EmergencyTask>> CreateEmergencyTask([FromBody] EmergencyTask task)
        {
            try
            {
                // توليد رقم البلاغ تلقائياً وبشكل فريد في حال لم يتم إرساله
                if (string.IsNullOrWhiteSpace(task.TaskNumber))
                {
                    task.TaskNumber = $"EMG-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                }

                task.CreatedAt = DateTime.UtcNow;
                task.Status = "PENDING";

                _context.EmergencyTasks.Add(task);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEmergencyTasks), new { id = task.Id }, task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"تعذر حفظ البلاغ الطارئ: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // POST: api/EmergencyTasks/5/respond
        [HttpPost("{id}/respond")]
        public async Task<IActionResult> RespondToTask(long id)
        {
            var task = await _context.EmergencyTasks.FindAsync(id);
            if (task == null) return NotFound("البلاغ غير موجود");

            task.RespondedAt = DateTime.UtcNow;
            task.Status = "IN_PROGRESS";
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/EmergencyTasks/5/delegate
        [HttpPost("{id}/delegate")]
        public async Task<IActionResult> DelegateTask(long id, [FromBody] DelegateTaskDto dto)
        {
            var task = await _context.EmergencyTasks.FindAsync(id);
            if (task == null) return NotFound("البلاغ غير موجود");

            task.AssignedTechnicianName = dto.TechnicianName;
            task.AssignmentNotes = dto.Notes;
            task.AssignedAt = DateTime.UtcNow;
            task.Status = "ASSIGNED";

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/EmergencyTasks/5/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteTask(long id, [FromBody] CompleteTaskDto dto)
        {
            var task = await _context.EmergencyTasks.FindAsync(id);
            if (task == null) return NotFound("البلاغ غير موجود");

            task.CompletedAt = DateTime.UtcNow;
            task.ClosureNotes = dto.ClosureNotes;
            task.ClosureProofMediaUrl = dto.ClosureProofMediaUrl;
            task.ClosedBy = dto.ClosedBy;
            task.Status = "CLOSED";

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class DelegateTaskDto
    {
        public string TechnicianName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CompleteTaskDto
    {
        public string ClosureNotes { get; set; } = string.Empty;
        public string? ClosureProofMediaUrl { get; set; }
        public string? ClosedBy { get; set; }
    }
}