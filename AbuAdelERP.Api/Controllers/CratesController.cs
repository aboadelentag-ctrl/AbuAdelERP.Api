using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using System.Security.Claims;

namespace AbuAdelERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CratesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CratesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. أرصدة الحاويات والأسبات اللحظية
        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            var inventory = await _context.CirculatingContainers.AsNoTracking().ToListAsync();
            return Ok(inventory);
        }

        // 2. سجل دورات الاستلام والتسليم
        [HttpGet("movements")]
        public async Task<IActionResult> GetMovements([FromQuery] string? status)
        {
            var query = _context.CrateMovements.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(m => m.Status == status);

            var list = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return Ok(list);
        }

        // 3. إصدار إذن تسليم وصرف أسبات (طرف أول مُسلِّم)
        [HttpPost("dispatch")]
        public async Task<IActionResult> Dispatch([FromBody] DispatchCrateDto dto)
        {
            if (dto.DispatchedQuantity <= 0)
                return BadRequest(new { message = "الكمية يجب أن تكون أكبر من الصفر!" });

            var empCode = User.FindFirstValue("EmployeeCode") ?? "EMP-088";
            var empName = User.FindFirstValue(ClaimTypes.Name) ?? "أمين المستودع";

            var movementNumber = $"CRT-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";

            var movement = new CrateMovement
            {
                MovementNumber = movementNumber,
                CreatedAt = DateTime.UtcNow,
                MovementType = dto.MovementType,
                ContainerName = dto.ContainerName,
                DispatchedQuantity = dto.DispatchedQuantity,
                SourceLocation = dto.SourceLocation,
                DispatchedByCode = empCode,
                DispatchedByName = empName,
                DestinationLocation = dto.DestinationLocation,
                Notes = dto.Notes,
                Status = "PENDING_RECEIPT"
            };

            _context.CrateMovements.Add(movement);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إصدار إذن التسليم بنجاح، بانتظار تأكيد المستلم", movement });
        }

        // 4. فحص وتأكيد الاستلام الميداني (طرف ثانٍ مُستلِم)
        [HttpPost("movements/{id:long}/receive")]
        public async Task<IActionResult> Receive(long id, [FromBody] ReceiveCrateDto dto)
        {
            var movement = await _context.CrateMovements.FirstOrDefaultAsync(m => m.Id == id);
            if (movement == null) return NotFound(new { message = "إذن الحركة غير موجود" });

            if (movement.Status != "PENDING_RECEIPT")
                return BadRequest(new { message = "هذه الحركة تم استلامها أو معالجتها مسبقاً!" });

            var empCode = User.FindFirstValue("EmployeeCode") ?? "EMP-045";
            var empName = User.FindFirstValue(ClaimTypes.Name) ?? "المستلم الميداني";

            // تطبيق مبدأ فصل المهام (SoD)
            if (movement.DispatchedByCode.Equals(empCode, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "ممنوع رقابياً: لا يجوز للطرف المُسلِّم تأكيد الاستلام لنفسه (Segregation of Duties)!" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var container = await _context.CirculatingContainers
                    .FirstOrDefaultAsync(c => c.ContainerName == movement.ContainerName);

                movement.ReceivedQuantity = dto.ActualReceivedQuantity;
                movement.DamagedCount = dto.DamagedCount;
                movement.ShortageQuantity = movement.DispatchedQuantity - dto.ActualReceivedQuantity;
                movement.ReceivedAt = DateTime.UtcNow;
                movement.ReceivedByCode = empCode;
                movement.ReceivedByName = empName;
                movement.ReceiveNotes = dto.ReceiveNotes;

                if (movement.ShortageQuantity > 0 || dto.DamagedCount > 0)
                {
                    movement.Status = "DISPUTED";
                    if (container != null)
                    {
                        container.DisputedQuantity += movement.ShortageQuantity;
                        container.DamagedQuantity += dto.DamagedCount;
                    }
                }
                else
                {
                    movement.Status = "COMPLETED";

                    if (container != null)
                    {
                        if (movement.MovementType == "DISPATCH_BRANCH")
                        {
                            container.InPackagingWarehouseQuantity -= movement.DispatchedQuantity;
                            container.WithDriversAndBranchesQuantity += movement.DispatchedQuantity;
                        }
                        else if (movement.MovementType == "RETURN_FACTORY")
                        {
                            container.WithDriversAndBranchesQuantity -= movement.DispatchedQuantity;
                            container.InPackagingWarehouseQuantity += movement.DispatchedQuantity;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = movement.Status == "DISPUTED" ? "تم فتح محضر نزاع رسمي لوجود عجز" : "تم تأكيد الاستلام واكتمال الدورة بنجاح", movement });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "خطأ أثناء معالجة الاستلام", error = ex.Message });
            }
        }

        // 5. فض وتسوية النزاع والعجز (لجنة الرقابة والتحكيم)
        [HttpPost("movements/{id:long}/settle-dispute")]
        public async Task<IActionResult> SettleDispute(long id, [FromBody] SettleDisputeDto dto)
        {
            var movement = await _context.CrateMovements.FirstOrDefaultAsync(m => m.Id == id);
            if (movement == null) return NotFound();

            if (movement.Status != "DISPUTED")
                return BadRequest(new { message = "هذه الحركة ليست بحالة نزاع!" });

            var empCode = User.FindFirstValue("EmployeeCode") ?? "ADMIN-01";
            var empName = User.FindFirstValue(ClaimTypes.Name) ?? "رئيس لجنة فض المنازعات";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                movement.Status = "RESOLVED";
                movement.ArbitrationDecision = dto.ArbitrationDecision;
                movement.ArbitrationText = dto.ArbitrationText;
                movement.FinancialPenalty = dto.FinancialPenalty;
                movement.ArbitratedByCode = empCode;
                movement.ArbitratedByName = empName;

                var container = await _context.CirculatingContainers
                    .FirstOrDefaultAsync(c => c.ContainerName == movement.ContainerName);

                if (container != null)
                {
                    container.DisputedQuantity = Math.Max(0, container.DisputedQuantity - movement.ShortageQuantity);
                    container.TotalFactoryQuantity = Math.Max(0, container.TotalFactoryQuantity - movement.ShortageQuantity);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "تم اعتماد فض النزاع وتسوية العهدة بنجاح", movement });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "خطأ أثناء تسوية النزاع", error = ex.Message });
            }
        }
    }
}