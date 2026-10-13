using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmergencyTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmergencyTasksController(AppDbContext context)
        {
            _context = context;
        }

        // 1. استعراض البلاغات (لشاشة الطوارئ وشاشة التقارير SLA)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? priority)
        {
            try
            {
                var query = _context.EmergencyTasks.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(t => t.Status == status);

                if (!string.IsNullOrWhiteSpace(priority))
                    query = query.Where(t => t.Priority == priority);

                var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استرجاع البلاغات: {ex.Message}");
            }
        }

        // 2. إطلاق بلاغ طارئ جديد
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmergencyTaskDto dto)
        {
            try
            {
                var task = new EmergencyTask
                {
                    TaskNumber = !string.IsNullOrWhiteSpace(dto.TaskNumber)
                        ? dto.TaskNumber
                        : $"EMG-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}",
                    Title = dto.Title,
                    Description = dto.Description ?? "",
                    SenderDepartment = dto.SenderDepartment,
                    SenderEmployeeCode = dto.SenderEmployeeCode,
                    SenderEmployeeName = dto.SenderEmployeeName,
                    TargetDepartment = !string.IsNullOrWhiteSpace(dto.TargetDepartment)
                        ? dto.TargetDepartment
                        : "إدارة الصيانة والتشغيل المركزي",
                    TargetEmployeeName = dto.TargetEmployeeName,
                    Priority = !string.IsNullOrWhiteSpace(dto.Priority) ? dto.Priority : "HIGH",
                    MediaType = !string.IsNullOrWhiteSpace(dto.MediaType) ? dto.MediaType : "TEXT",
                    MediaUrl = dto.MediaUrl,
                    MediaFileName = dto.MediaFileName,
                    TargetResolutionMinutes = dto.TargetResolutionMinutes > 0 ? dto.TargetResolutionMinutes : 30,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    AssetCode = dto.AssetCode
                };

                // إذا كان البلاغ لماكينة ➔ تحويل حالتها فوراً لـ UNDER_MAINTENANCE وتوثيق السجل
                if (!string.IsNullOrWhiteSpace(dto.AssetCode))
                {
                    var asset = await _context.FixedAssets.Include(a => a.HistoryLogs).FirstOrDefaultAsync(a => a.AssetCode == dto.AssetCode);
                    if (asset != null)
                    {
                        asset.Status = "UNDER_MAINTENANCE";
                        asset.ActiveSOSTaskNumber = task.TaskNumber;

                        asset.HistoryLogs.Add(new AssetHistoryLog
                        {
                            Date = DateTime.UtcNow,
                            EventType = "عطل طارئ (SOS)",
                            Description = $"بلاغ عطل رقم ({task.TaskNumber}): {task.Title}",
                            PerformedByCode = dto.SenderEmployeeCode,
                            PerformedByName = dto.SenderEmployeeName,
                            CostImpact = "توقف إنتاج"
                        });
                    }
                }

                _context.EmergencyTasks.Add(task);
                await _context.SaveChangesAsync();

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"فشل إنشاء البلاغ: {ex.Message}");
            }
        }

        // 3. تكليف فني صيانة
        [HttpPost("{id:long}/delegate")]
        [HttpPut("{id:long}/assign")]
        public async Task<IActionResult> DelegateTask(long id, [FromBody] DelegateTaskDto dto)
        {
            var task = await _context.EmergencyTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound("البلاغ غير موجود");

            task.Status = "ASSIGNED";
            task.AssignedTechnicianCode = dto.TechnicianCode;
            task.AssignedTechnicianName = dto.TechnicianName;
            task.AssignmentNotes = dto.Notes;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"تم تكليف الفني ({dto.TechnicianName}) بالمهمة بنجاح", task });
        }

        // 4. بدء التنفيذ الميداني
        [HttpPost("{id:long}/respond")]
        public async Task<IActionResult> RespondToTask(long id)
        {
            var task = await _context.EmergencyTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound("البلاغ غير موجود");

            task.Status = "IN_PROGRESS";
            task.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تسجيل بدء التنفيذ الميداني", task });
        }

        // 5. إغلاق البلاغ واستعادة تشغيل الماكينة
        [HttpPost("{id:long}/complete")]
        [HttpPut("{id:long}/resolve")]
        public async Task<IActionResult> CompleteTask(long id, [FromBody] CompleteTaskDto dto)
        {
            var task = await _context.EmergencyTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound("البلاغ غير موجود");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                task.Status = "CLOSED";
                task.CompletedAt = DateTime.UtcNow;
                task.ClosureNotes = dto.ClosureNotes;
                task.ClosureProofMediaUrl = dto.ClosureProofMediaUrl;
                task.ClosedBy = dto.ClosedBy;
                task.ClosedByCode = dto.ClosedByCode;

                if (!string.IsNullOrWhiteSpace(dto.CostImpact)) task.CostImpact = dto.CostImpact;
                if (!string.IsNullOrWhiteSpace(dto.SparePartsUsed)) task.SparePartsUsed = dto.SparePartsUsed;
                if (!string.IsNullOrWhiteSpace(dto.RootCause)) task.RootCause = dto.RootCause;
                if (dto.DowntimeMinutes > 0) task.DowntimeMinutes = dto.DowntimeMinutes;

                // إعادة الأصل تلقائياً إلى OPERATIONAL
                var asset = await _context.FixedAssets
                    .Include(a => a.HistoryLogs)
                    .FirstOrDefaultAsync(a => (!string.IsNullOrEmpty(task.AssetCode) && a.AssetCode == task.AssetCode) ||
                                              a.ActiveSOSTaskNumber == task.TaskNumber);

                if (asset != null)
                {
                    asset.Status = "OPERATIONAL";
                    asset.ActiveSOSTaskNumber = null;

                    asset.HistoryLogs.Add(new AssetHistoryLog
                    {
                        Date = DateTime.UtcNow,
                        EventType = "إصلاح عطل واستعادة تشغيل",
                        Description = $"تم إنهاء العطل ({task.TaskNumber}): {dto.ClosureNotes}",
                        PerformedByCode = !string.IsNullOrEmpty(dto.ClosedByCode) ? dto.ClosedByCode : task.AssignedTechnicianCode,
                        PerformedByName = !string.IsNullOrEmpty(dto.ClosedBy) ? dto.ClosedBy : task.AssignedTechnicianName,
                        CostImpact = !string.IsNullOrWhiteSpace(dto.CostImpact) ? dto.CostImpact : "صيانة عادية"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "تم إغلاق البلاغ وإعادة تشغيل الماكينة بنجاح ✓", task });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء معالجة إغلاق البلاغ: {ex.Message}");
            }
        }
    }

    public class CreateEmergencyTaskDto
    {
        public string? TaskNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SenderDepartment { get; set; } = string.Empty;
        public string SenderEmployeeCode { get; set; } = string.Empty;
        public string SenderEmployeeName { get; set; } = string.Empty;
        public string? TargetDepartment { get; set; }
        public string? TargetEmployeeName { get; set; }
        public string? Priority { get; set; }
        public string? MediaType { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaFileName { get; set; }
        public int TargetResolutionMinutes { get; set; } = 30;
        public string? AssetCode { get; set; }
    }

    public class DelegateTaskDto
    {
        public string TechnicianName { get; set; } = string.Empty;
        public string? TechnicianCode { get; set; }
        public string? Notes { get; set; }
    }

    public class CompleteTaskDto
    {
        public string ClosureNotes { get; set; } = string.Empty;
        public string? ClosureProofMediaUrl { get; set; }
        public string? ClosedBy { get; set; }
        public string? ClosedByCode { get; set; }
        public string? CostImpact { get; set; }
        public string? SparePartsUsed { get; set; }
        public string? RootCause { get; set; }
        public int DowntimeMinutes { get; set; }
    }
}