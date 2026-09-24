using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionOperationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductionOperationsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. إصدار وإنشاء أمر تشغيل جديد (يدعم صنف واحد أو عدة أصناف في نفس الأمر)
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateProductionOrder([FromBody] CreateOrderApiRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("بيانات الطلب غير صالحة.");
            }

            var itemsToProcess = request.Items != null && request.Items.Any()
                ? request.Items
                : (request.ItemId > 0 ? new List<CreateOrderItemLineDto> { new() { ItemId = request.ItemId, PlannedQuantity = request.PlannedQuantity, Unit = request.Unit } } : new List<CreateOrderItemLineDto>());

            if (!itemsToProcess.Any() || itemsToProcess.All(i => i.PlannedQuantity <= 0))
            {
                return BadRequest("يرجى إضافة صنف واحد على الأقل وتحديد كمية مخططة صحيحة.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var todayCode = DateTime.UtcNow.ToString("yyyyMMdd");
                var randomSuffix = new Random().Next(100, 999);
                var orderNumber = $"PRD-{todayCode}-{randomSuffix}";

                var newOrder = new ProductionOrder
                {
                    CompanyId = request.CompanyId > 0 ? request.CompanyId : 1,
                    WarehouseId = request.WarehouseId > 0 ? request.WarehouseId : 1,
                    OrderNumber = orderNumber,
                    OrderDate = request.OrderDate != default ? request.OrderDate : DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "قيد التشغيل",
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProductionOrders.Add(newOrder);
                await _context.SaveChangesAsync();

                int lineIndex = 1;
                foreach (var line in itemsToProcess)
                {
                    var item = await _context.Items.FindAsync(line.ItemId);
                    if (item == null) continue;

                    var newDetail = new ProductionOrderDetail
                    {
                        ProductionOrderId = newOrder.Id,
                        ItemId = item.Id,
                        PlannedQuantity = line.PlannedQuantity,
                        ProducedQuantity = 0,
                        Unit = !string.IsNullOrEmpty(line.Unit) ? line.Unit : item.Unit,
                        BatchNumber = $"LOT-{todayCode}-{item.ItemCode}-{randomSuffix}-{lineIndex++}",
                        Status = "قيد التشغيل",
                        Notes = line.Notes ?? $"أمر تشغيل مسار: {item.ItemType}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ProductionOrderDetails.Add(newDetail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = $"تم إصدار أمر التشغيل بنجاح برقم: {orderNumber} ويحتوي على ({itemsToProcess.Count}) أصناف.",
                    OrderId = newOrder.Id,
                    OrderNumber = orderNumber
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"حدث خطأ أثناء إصدار أمر التشغيل: {ex.Message}");
            }
        }

        // 2. جلب أوامر التشغيل المفتوحة مع تفاصيل الصنف والكميات
        [HttpGet("active-orders")]
        public async Task<ActionResult<IEnumerable<ProductionOrderOverviewDto>>> GetActiveOrders([FromQuery] string? orderType)
        {
            var list = await _context.ProductionOrders
                .AsNoTracking()
                .Where(o => o.Status != "Completed" && o.Status != "Cancelled")
                .Include(o => o.ProductionOrderDetails)
                    .ThenInclude(d => d.Item)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .ToListAsync();

            var result = list.Select(o => new ProductionOrderOverviewDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status,
                WarehouseId = o.WarehouseId,
                Details = o.ProductionOrderDetails.Select(d => new OrderDetailCardDto
                {
                    DetailId = d.Id,
                    ItemId = d.ItemId,
                    ItemCode = d.Item?.ItemCode ?? "",
                    ItemName = d.Item?.ItemName ?? "",
                    ItemType = d.Item?.ItemType ?? "منتج تام",
                    PlannedQuantity = d.PlannedQuantity,
                    ProducedQuantity = d.ProducedQuantity,
                    Unit = d.Unit,
                    BatchNumber = d.BatchNumber,
                    Status = d.Status
                }).ToList()
            }).ToList();

            if (!string.IsNullOrEmpty(orderType))
            {
                result = result.Where(r => r.Details.Any(d => d.ItemType == orderType)).ToList();
            }

            return Ok(result);
        }

        // 3. جلب طاقم الصالة تلقائياً مع مسمياتهم وأجر ساعتهم الاسترشادي
        [HttpGet("department-crew/{departmentId}")]
        public async Task<ActionResult<IEnumerable<CrewMemberDto>>> GetDepartmentCrew(long departmentId)
        {
            var crew = await _context.Employees
                .AsNoTracking()
                .Where(e => e.DepartmentId == departmentId && e.Active)
                .Include(e => e.JobTitle)
                .Select(e => new CrewMemberDto
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.Name,
                    JobTitleName = e.JobTitle != null ? e.JobTitle.TitleName : "عامل تشغيل",
                    HourlyRate = e.JobTitle != null ? e.JobTitle.HourlyRate : 0,
                    AttendanceStatus = "حاضر",
                    HoursWorked = 8,
                    CheckIn = "07:00",
                    CheckOut = "15:00"
                })
                .ToListAsync();

            return Ok(crew);
        }

        // 4. جلب جميع العمال بالمصنع (لاستخدامهم كعمال دعم منتدبين)
        [HttpGet("all-active-employees")]
        public async Task<ActionResult<IEnumerable<CrewMemberDto>>> GetAllActiveEmployees()
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Active)
                .Include(e => e.JobTitle)
                .Select(e => new CrewMemberDto
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.Name,
                    JobTitleName = e.JobTitle != null ? e.JobTitle.TitleName : "عامل تشغيل",
                    HourlyRate = e.JobTitle != null ? e.JobTitle.HourlyRate : 0,
                    AttendanceStatus = "حاضر",
                    HoursWorked = 8,
                    CheckIn = "07:00",
                    CheckOut = "15:00"
                })
                .ToListAsync();

            return Ok(employees);
        }

        // 5. تقفيل الدفعة واحتساب التكاليف بالمعاملة الذرية
        [HttpPost("finalize-batch")]
        public async Task<IActionResult> FinalizeBatchExecution([FromBody] BatchFinalizeRequestDto request)
        {
            if (request == null || request.OrderDetailId <= 0 || request.ProducedQuantity <= 0)
            {
                return BadRequest("بيانات الدفعة غير صحيحة أو الكمية المنتجة تساوي صفر.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var detail = await _context.ProductionOrderDetails
                    .Include(d => d.Item)
                    .Include(d => d.ProductionOrder)
                    .FirstOrDefaultAsync(d => d.Id == request.OrderDetailId);

                if (detail == null)
                {
                    return NotFound("لم يتم العثور على سطر أمر التشغيل المطلوب.");
                }

                detail.ProducedQuantity = request.ProducedQuantity;
                detail.BatchNumber = request.BatchNumber;
                detail.Status = "Completed";
                detail.UpdatedAt = DateTime.UtcNow;

                decimal totalLaborCost = 0;
                if (request.CrewMembers != null && request.CrewMembers.Any())
                {
                    foreach (var member in request.CrewMembers)
                    {
                        var isPresent = member.AttendanceStatus == "حاضر";
                        var hours = isPresent ? member.HoursWorked : 0;
                        var laborCost = hours * member.HourlyRate;

                        if (isPresent)
                        {
                            totalLaborCost += laborCost;
                        }

                        TimeOnly? checkInTime = TimeOnly.TryParse(member.CheckIn, out var cin) ? cin : new TimeOnly(7, 0);
                        TimeOnly? checkOutTime = TimeOnly.TryParse(member.CheckOut, out var cout) ? cout : new TimeOnly(15, 0);

                        var log = new ProductivityLog
                        {
                            ProductionOrderId = detail.ProductionOrderId,
                            EmployeeId = member.EmployeeId,
                            WorkDate = DateTime.UtcNow,
                            AttendanceStatus = member.AttendanceStatus,
                            CheckIn = checkInTime,
                            CheckOut = checkOutTime,
                            HoursWorked = hours,
                            HourlyRate = member.HourlyRate,
                            LaborCost = laborCost,
                            ProducedQuantity = isPresent ? request.ProducedQuantity : 0,
                            WasteQuantity = member.ScrapAttributed,
                            WasteType = member.ScrapReason,
                            Notes = member.Notes
                        };

                        _context.Set<ProductivityLog>().Add(log);
                    }
                }

                decimal normalScrapCost = request.NormalScrapQuantity * request.StandardUnitCost;
                decimal totalBatchCost = request.ActualRawMaterialCost + totalLaborCost + request.OverheadCost + normalScrapCost;
                decimal unitActualCost = request.ProducedQuantity > 0 ? (totalBatchCost / request.ProducedQuantity) : 0;

                detail.WorkingHours = request.CrewMembers?.Where(c => c.AttendanceStatus == "حاضر").Sum(c => c.HoursWorked) ?? 0;
                detail.WasteQuantity = request.NormalScrapQuantity + request.AbnormalScrapQuantity;
                detail.Notes = $"التكلفة المحسوبة للوحدة: {unitActualCost:F2} ج.م | إجمالي العمالة: {totalLaborCost:F2} ج.م";

                var allDetails = await _context.ProductionOrderDetails
                    .Where(d => d.ProductionOrderId == detail.ProductionOrderId && d.Id != detail.Id)
                    .ToListAsync();

                if (allDetails.All(d => d.Status == "Completed"))
                {
                    detail.ProductionOrder!.Status = "Completed";
                    detail.ProductionOrder.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    detail.ProductionOrder!.Status = "In_Progress";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "تم اعتماد وتقفيل الدفعة وترصيد التكاليف بنجاح.",
                    BatchNumber = detail.BatchNumber,
                    CalculatedUnitCost = unitActualCost,
                    TotalLaborCost = totalLaborCost,
                    TotalBatchCost = totalBatchCost
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"حدث خطأ أثناء تقفيل الدفعة: {ex.Message}");
            }
        }
    }

    // --- API Request DTOs ---
    public class CreateOrderApiRequestDto
    {
        public long CompanyId { get; set; } = 1;
        public long WarehouseId { get; set; } = 1;
        public long? DepartmentId { get; set; }
        public DateOnly OrderDate { get; set; }
        public string? Notes { get; set; }

        public List<CreateOrderItemLineDto> Items { get; set; } = new();

        public long ItemId { get; set; }
        public decimal PlannedQuantity { get; set; }
        public string? Unit { get; set; }
    }

    public class CreateOrderItemLineDto
    {
        public long ItemId { get; set; }
        public decimal PlannedQuantity { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class CrewMemberDto
    {
        public long EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string JobTitleName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public string AttendanceStatus { get; set; } = "حاضر";
        public string? CheckIn { get; set; } = "07:00";
        public string? CheckOut { get; set; } = "15:00";
        public decimal HoursWorked { get; set; }
        public decimal ScrapAttributed { get; set; } = 0;
        public string? ScrapReason { get; set; }
        public string? Notes { get; set; }
    }

    public class ProductionOrderOverviewDto
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public List<OrderDetailCardDto> Details { get; set; } = new();
    }

    public class OrderDetailCardDto
    {
        public long DetailId { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; }
        public decimal ProducedQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BatchFinalizeRequestDto
    {
        public long OrderDetailId { get; set; }
        public decimal ProducedQuantity { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public decimal ActualRawMaterialCost { get; set; }
        public decimal StandardUnitCost { get; set; }
        public decimal OverheadCost { get; set; } = 0;
        public decimal NormalScrapQuantity { get; set; } = 0;
        public decimal AbnormalScrapQuantity { get; set; } = 0;
        public List<CrewMemberDto>? CrewMembers { get; set; }
    }
}