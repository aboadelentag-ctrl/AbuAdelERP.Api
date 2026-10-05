using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionOperationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductionOperationsController(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. الدورة المغلقة: سحب صالات الإنتاج ومخازنها وعمالها حصراً
        // ========================================================

        [HttpGet("lookups")]
        public async Task<IActionResult> GetInitialLookups()
        {
            try
            {
                var departments = await _context.Departments
                    .AsNoTracking()
                    .Where(d => d.DepartmentName.Contains("قسم")
                             || d.DepartmentName.Contains("صالة")
                             || d.DepartmentName.Contains("تورت")
                             || d.DepartmentName.Contains("جاتوه")
                             || d.DepartmentName.Contains("شرقي")
                             || d.DepartmentName.Contains("اسبشيال")
                             || d.DepartmentName.Contains("مخبوزات")
                             || d.DepartmentName.Contains("كرواسون"))
                    .Where(d => !d.DepartmentName.Contains("إدارة")
                             && !d.DepartmentName.Contains("العليا")
                             && !d.DepartmentName.Contains("المالية")
                             && !d.DepartmentName.Contains("الموارد")
                             && !d.DepartmentName.Contains("المشتريات")
                             && !d.DepartmentName.Contains("المبيعات"))
                    .OrderBy(d => d.DepartmentName)
                    .Select(d => new { d.Id, d.DepartmentName, d.DepartmentCode })
                    .ToListAsync();

                var prodDeptIds = departments.Select(d => d.Id).ToList();

                var warehouses = await _context.Warehouses
                    .AsNoTracking()
                    .Where(w => w.WarehouseName.Contains("صالة")
                             || w.WarehouseName.Contains("تشغيل")
                             || w.WarehouseName.Contains("قسم")
                             || w.WarehouseName.Contains("شرقي")
                             || w.WarehouseName.Contains("جاتوه")
                             || w.WarehouseName.Contains("تورت")
                             || w.WarehouseName.Contains("اسبشيال"))
                    .OrderBy(w => w.WarehouseName)
                    .Select(w => new { w.Id, w.WarehouseName, w.WarehouseCode })
                    .ToListAsync();

                var items = await _context.Items
                    .AsNoTracking()
                    .Where(i => i.Active == true && (i.ItemType == "منتج تام" || i.ItemType == "نصف مصنع" || i.ItemType == "عجين"))
                    .Select(i => new
                    {
                        i.Id,
                        i.ItemCode,
                        i.ItemName,
                        i.ItemType,
                        i.Unit,
                        i.DepartmentId,
                        DepartmentName = i.Department != null ? i.Department.DepartmentName : "",
                        CurrentStock = _context.StockBalances.Where(s => s.ItemId == i.Id).Sum(s => s.Quantity)
                    })
                    .ToListAsync();

                var employees = await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.Active && e.DepartmentId.HasValue && prodDeptIds.Contains(e.DepartmentId.Value))
                    .Select(e => new
                    {
                        e.Id,
                        e.EmployeeCode,
                        e.Name,
                        e.DepartmentId,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : "صالة إنتاج",
                        JobTitle = e.JobTitle != null ? e.JobTitle.TitleName : "عامل إنتاج"
                    })
                    .ToListAsync();

                return Ok(new { departments, warehouses, items, employees });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات صالات الإنتاج: {ex.Message}");
            }
        }

        // ========================================================
        // 2. أوامر التشغيل المفتوحة
        // ========================================================

        [HttpGet("active-orders")]
        public async Task<IActionResult> GetActiveOrders()
        {
            try
            {
                var orders = await _context.ProductionOrders
                    .AsNoTracking()
                    .Include(o => o.Warehouse)
                    .Include(o => o.ProductionOrderDetails)
                        .ThenInclude(d => d.Item)
                            .ThenInclude(i => i.Department)
                    .Include(o => o.ProductionOrderDetails)
                        .ThenInclude(d => d.Recipe)
                    .OrderByDescending(o => o.Id)
                    .Select(o => new
                    {
                        OrderId = o.Id,
                        o.OrderNumber,
                        o.OrderDate,
                        o.Status,
                        o.WarehouseId,
                        WarehouseName = o.Warehouse != null ? o.Warehouse.WarehouseName : "مخزن صالة التشغيل",
                        DepartmentId = o.ProductionOrderDetails.Select(d => d.Item.DepartmentId).FirstOrDefault() ?? 0,
                        DepartmentName = o.ProductionOrderDetails.Select(d => d.Item.Department != null ? d.Item.Department.DepartmentName : "").FirstOrDefault() ?? "صالة إنتاج",
                        o.Notes,
                        Details = o.ProductionOrderDetails.Select(d => new
                        {
                            DetailId = d.Id,
                            d.ItemId,
                            ItemCode = d.Item.ItemCode,
                            ItemName = d.Item.ItemName,
                            ItemType = d.Item.ItemType,
                            d.PlannedQuantity,
                            d.ProducedQuantity,
                            d.WasteQuantity,
                            Unit = d.Item.Unit,
                            RecipeId = d.RecipeId,
                            RecipeName = d.Recipe != null ? d.Recipe.RecipeName : "",
                            Status = d.Status,
                            BatchNumber = d.Notes
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب أوامر التشغيل: {ex.Message}");
            }
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateBatchOrderDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return BadRequest("يجب تحديد صنف واحد على الأقل في أمر التشغيل.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = dto.CompanyId > 0 ? dto.CompanyId : 1;
                var orderNumber = !string.IsNullOrWhiteSpace(dto.OrderNumber)
                    ? dto.OrderNumber
                    : $"PRD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";

                var order = new ProductionOrder
                {
                    CompanyId = companyId,
                    OrderNumber = orderNumber,
                    OrderDate = dto.OrderDate != default ? dto.OrderDate : DateOnly.FromDateTime(DateTime.UtcNow),
                    WarehouseId = dto.WarehouseId,
                    Status = "PENDING_EXECUTION",
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductionOrders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var line in dto.Items)
                {
                    var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.ProductItemId == line.ItemId && r.Active == true);

                    var detail = new ProductionOrderDetail
                    {
                        ProductionOrderId = order.Id,
                        ItemId = line.ItemId,
                        RecipeId = recipe != null ? recipe.Id : (line.RecipeId > 0 ? line.RecipeId : null),
                        PlannedQuantity = line.PlannedQuantity,
                        ProducedQuantity = 0,
                        WasteQuantity = 0,
                        Status = "PENDING",
                        Notes = line.Notes
                    };
                    _context.ProductionOrderDetails.Add(detail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "تم إصدار أمر التشغيل بنجاح", orderId = order.Id, orderNumber = order.OrderNumber });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"فشل إصدار أمر التشغيل: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 3. التنفيذ الميداني وتقفيل كامل بنود الأمر
        // ========================================================

        [HttpGet("order-execution-full/{orderId:long}")]
        public async Task<IActionResult> GetOrderExecutionFull(long orderId)
        {
            try
            {
                var order = await _context.ProductionOrders
                    .AsNoTracking()
                    .Include(o => o.Warehouse)
                    .Include(o => o.ProductionOrderDetails)
                        .ThenInclude(d => d.Item)
                    .Include(o => o.ProductionOrderDetails)
                        .ThenInclude(d => d.Recipe)
                            .ThenInclude(r => r.RecipeLines)
                                .ThenInclude(rl => rl.ComponentItem)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return NotFound("أمر التشغيل غير موجود.");

                var items = order.ProductionOrderDetails.Select(d => new
                {
                    DetailId = d.Id,
                    d.ItemId,
                    ItemCode = d.Item.ItemCode,
                    ItemName = d.Item.ItemName,
                    ItemType = d.Item.ItemType,
                    Unit = d.Item.Unit,
                    PlannedQuantity = d.PlannedQuantity,
                    ProducedQuantity = d.ProducedQuantity > 0 ? d.ProducedQuantity : d.PlannedQuantity,
                    NormalScrapQuantity = 0m,
                    AbnormalScrapQuantity = 0m,
                    ScrapReason = "",
                    Status = d.Status,
                    BatchNumber = !string.IsNullOrEmpty(d.Notes) ? d.Notes : $"LOT-{DateTime.UtcNow:yyyyMMdd}-{d.Item.ItemCode}-{d.Id}",
                    ExpiryDate = DateTime.UtcNow.AddDays(Convert.ToDouble(d.Item.ShelfLifeDays.HasValue && d.Item.ShelfLifeDays.Value > 0 ? d.Item.ShelfLifeDays.Value : 4)),
                    Materials = d.Recipe != null ? d.Recipe.RecipeLines.Select(rl => new
                    {
                        ComponentItemId = rl.ComponentItemId,
                        ComponentCode = rl.ComponentItem.ItemCode,
                        ComponentName = rl.ComponentItem.ItemName,
                        Unit = rl.Unit,
                        RequiredQuantity = d.Recipe.BatchQuantity > 0 ? (rl.Quantity / d.Recipe.BatchQuantity) * d.PlannedQuantity : (rl.Quantity * d.PlannedQuantity),
                        ActualConsumedQuantity = d.Recipe.BatchQuantity > 0 ? (rl.Quantity / d.Recipe.BatchQuantity) * d.PlannedQuantity : (rl.Quantity * d.PlannedQuantity),
                        UnitCost = rl.ComponentItem.BasePrice
                    }).ToList() : new()
                }).ToList();

                var result = new
                {
                    order.Id,
                    order.OrderNumber,
                    order.OrderDate,
                    order.Status,
                    WarehouseName = order.Warehouse != null ? order.Warehouse.WarehouseName : "مخزن الصالة",
                    Items = items
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج بيانات التنفيذ: {ex.Message}");
            }
        }

        [HttpPost("finalize-order-complete")]
        public async Task<IActionResult> FinalizeOrderComplete([FromBody] CompleteOrderFinalizeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.ProductionOrders
                    .Include(o => o.ProductionOrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

                if (order == null) return NotFound("أمر التشغيل غير موجود.");

                foreach (var itemDto in dto.Items)
                {
                    var detail = order.ProductionOrderDetails.FirstOrDefault(d => d.Id == itemDto.DetailId);
                    if (detail != null)
                    {
                        detail.ProducedQuantity = itemDto.ProducedQuantity;
                        detail.WasteQuantity = itemDto.NormalScrapQuantity + itemDto.AbnormalScrapQuantity;
                        detail.Status = "Completed";
                        detail.Notes = itemDto.BatchNumber;

                        if (detail.WasteQuantity > 0)
                        {
                            var waste = new WasteRecord
                            {
                                CompanyId = order.CompanyId,
                                ProductionOrderId = order.Id,
                                ItemId = detail.ItemId,
                                WarehouseId = order.WarehouseId,
                                Quantity = detail.WasteQuantity,
                                WasteDate = DateTime.UtcNow,
                                WasteReason = itemDto.AbnormalScrapQuantity > 0 ? $"هالك غير طبيعي ({itemDto.ScrapReason})" : "هالك تشغيل طبيعي مسموح به",
                                Notes = $"تشغيلة لوط: {itemDto.BatchNumber}"
                            };
                            _context.WasteRecords.Add(waste);
                        }
                    }
                }

                order.Status = "COMPLETED";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "تم تقفيل واعتماد أمر التشغيل بالكامل وترصيد اللوطات والهالك ✓" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ في تقفيل أمر التشغيل: {ex.Message}");
            }
        }

        // ========================================================
        // 4. ترصيد إنتاجية الوردية وحفظ الحضور وساعات العمل
        // ========================================================

        [HttpPost("submit-shift-productivity")]
        public async Task<IActionResult> SubmitShiftProductivity([FromBody] ShiftProductivityDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var presentCrew = dto.CrewMembers
                    .Where(c => c.AttendanceStatus == "حاضر" && c.HoursWorked > 0)
                    .ToList();

                if (!presentCrew.Any())
                    return BadRequest("لم يتم تحديد أي عمال حاضرين لديهم ساعات عمل فعلية بالوردية.");

                ProductionOrder? targetOrder = null;
                if (dto.ProductionOrderId > 0)
                {
                    targetOrder = await _context.ProductionOrders.FirstOrDefaultAsync(o => o.Id == dto.ProductionOrderId);
                }

                if (targetOrder == null)
                {
                    targetOrder = await _context.ProductionOrders.OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                }

                if (targetOrder == null)
                {
                    return BadRequest("لا يوجد أمر تشغيل مسجل في النظام لربط الوردية به. يرجى إصدار أمر تشغيل أولاً.");
                }

                decimal perWorkerProduced = dto.TotalProducedQuantity > 0
                    ? Math.Round(dto.TotalProducedQuantity / presentCrew.Count, 2)
                    : 0;

                var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

                TimeOnly.TryParse(dto.StartTime, out var checkInTime);
                TimeOnly.TryParse(dto.EndTime, out var checkOutTime);

                foreach (var worker in presentCrew)
                {
                    var rate = worker.HourlyRate > 0 ? worker.HourlyRate : 50m;
                    var cost = Math.Round(worker.HoursWorked * rate, 2);

                    var log = new ProductivityLog
                    {
                        ProductionOrderId = targetOrder.Id,
                        EmployeeId = worker.EmployeeId,
                        WorkDate = nowUtc,
                        AttendanceStatus = "حاضر",
                        CheckIn = checkInTime != default ? checkInTime : (TimeOnly?)null,
                        CheckOut = checkOutTime != default ? checkOutTime : (TimeOnly?)null,
                        HoursWorked = worker.HoursWorked,
                        HourlyRate = rate,
                        LaborCost = cost,
                        ProducedQuantity = perWorkerProduced,
                        WasteQuantity = 0,
                        WasteType = null,
                        Notes = $"وردية: {dto.ShiftName} - ساعات فعلية: {worker.HoursWorked}س - تكلفة: {cost}ج.م"
                    };
                    _context.ProductivityLogs.Add(log);
                }

                var absentCrew = dto.CrewMembers.Where(c => c.AttendanceStatus != "حاضر" || c.HoursWorked <= 0).ToList();
                foreach (var worker in absentCrew)
                {
                    var log = new ProductivityLog
                    {
                        ProductionOrderId = targetOrder.Id,
                        EmployeeId = worker.EmployeeId,
                        WorkDate = nowUtc,
                        AttendanceStatus = worker.AttendanceStatus,
                        CheckIn = null,
                        CheckOut = null,
                        HoursWorked = 0,
                        HourlyRate = worker.HourlyRate,
                        LaborCost = 0,
                        ProducedQuantity = 0,
                        WasteQuantity = 0,
                        WasteType = null,
                        Notes = $"[{worker.AttendanceStatus}] وردية: {dto.ShiftName}"
                    };
                    _context.ProductivityLogs.Add(log);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = $"تم اعتماد الوردية بنجاح: ({presentCrew.Count}) حاضر، ({absentCrew.Count}) غائب/إجازة على أمر [{targetOrder.OrderNumber}] ✓" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var detailedError = ex.InnerException?.InnerException?.Message
                                 ?? ex.InnerException?.Message
                                 ?? ex.Message;
                return StatusCode(500, $"خطأ في تسجيل إنتاجية الوردية: {detailedError}");
            }
        }

        // ========================================================
        // 5. مركز التقارير واليوميات ومؤشرات كفاءة الصالات
        // ========================================================

        [HttpGet("kpis")]
        public async Task<IActionResult> GetDepartmentKPIs([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] long? departmentId)
        {
            try
            {
                var startUtc = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                var query = _context.ProductivityLogs
                    .AsNoTracking()
                    .Include(p => p.Employee)
                        .ThenInclude(e => e.Department)
                    .Where(p => p.WorkDate >= startUtc && p.WorkDate <= endUtc);

                if (departmentId.HasValue && departmentId.Value > 0)
                {
                    query = query.Where(p => p.Employee != null && p.Employee.DepartmentId == departmentId.Value);
                }

                var kpis = await query
                    .GroupBy(p => p.Employee != null && p.Employee.Department != null ? p.Employee.Department.DepartmentName : "صالة إنتاج عامة")
                    .Select(g => new
                    {
                        DepartmentName = g.Key,
                        TotalHours = g.Sum(x => x.HoursWorked),
                        TotalProduced = g.Sum(x => x.ProducedQuantity),
                        TotalLaborCost = g.Sum(x => x.LaborCost),
                        ProductivityPerHour = g.Sum(x => x.HoursWorked) > 0
                            ? Math.Round(g.Sum(x => x.ProducedQuantity) / g.Sum(x => x.HoursWorked), 2)
                            : 0,
                        EstimatedWaste = Math.Round(g.Sum(x => x.ProducedQuantity) * 0.02m, 2)
                    })
                    .ToListAsync();

                return Ok(kpis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج مؤشرات الأداء: {ex.Message}");
            }
        }

        [HttpGet("detailed-attendance-logs")]
        public async Task<IActionResult> GetDetailedAttendanceLogs([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] long? departmentId)
        {
            try
            {
                var startUtc = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                var query = _context.ProductivityLogs
                    .AsNoTracking()
                    .Include(p => p.Employee)
                        .ThenInclude(e => e.Department)
                    .Where(p => p.WorkDate >= startUtc && p.WorkDate <= endUtc);

                if (departmentId.HasValue && departmentId.Value > 0)
                {
                    query = query.Where(p => p.Employee != null && p.Employee.DepartmentId == departmentId.Value);
                }

                var logs = await query
                    .OrderByDescending(p => p.WorkDate)
                    .Select(p => new
                    {
                        p.Id,
                        LogDate = p.WorkDate,
                        EmployeeCode = p.Employee != null ? p.Employee.EmployeeCode : "EMP",
                        EmployeeName = p.Employee != null ? p.Employee.Name : "غير معروف",
                        DepartmentName = p.Employee != null && p.Employee.Department != null ? p.Employee.Department.DepartmentName : "صالة الإنتاج",
                        AttendanceStatus = p.AttendanceStatus,
                        HoursWorked = p.HoursWorked,
                        HourlyRate = p.HourlyRate,
                        LaborCost = p.LaborCost,
                        ProducedQuantity = p.ProducedQuantity,
                        ProductivityPerHour = p.HoursWorked > 0 ? Math.Round(p.ProducedQuantity / p.HoursWorked, 2) : 0,
                        Notes = p.Notes ?? ""
                    })
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب يوميات الحضور والإنتاج: {ex.Message}");
            }
        }

        [HttpGet("employee-dossier/{employeeId:long}")]
        public async Task<IActionResult> GetEmployeeDossier(long employeeId)
        {
            try
            {
                var emp = await _context.Employees
                    .AsNoTracking()
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (emp == null) return NotFound("الموظف غير موجود.");

                var logs = await _context.ProductivityLogs
                    .AsNoTracking()
                    .Where(p => p.EmployeeId == employeeId)
                    .ToListAsync();

                var totalHours = logs.Sum(l => l.HoursWorked);
                var totalProduced = logs.Sum(l => l.ProducedQuantity);
                var totalLaborCost = logs.Sum(l => l.LaborCost);
                var totalOrders = logs.Select(l => l.ProductionOrderId).Distinct().Count();
                var prodPerHour = totalHours > 0 ? Math.Round(totalProduced / totalHours, 2) : 0;

                string eval = prodPerHour >= 40 ? "ممتاز ⭐⭐⭐" : (prodPerHour >= 20 ? "جيد جداً ⭐⭐" : "يحتاج مراجعة وتدريب ⚠️");

                var dossier = new
                {
                    EmployeeName = emp.Name,
                    EmployeeCode = emp.EmployeeCode,
                    Department = emp.Department != null ? emp.Department.DepartmentName : "صالة إنتاج",
                    TotalOrdersExecuted = totalOrders,
                    TotalQuantityProduced = totalProduced,
                    TotalWorkingHours = totalHours,
                    TotalLaborCost = totalLaborCost,
                    ProductionPerHour = prodPerHour,
                    TotalWaste = Math.Round(totalProduced * 0.015m, 2),
                    WastePercentage = "1.5%",
                    Evaluation = eval
                };

                return Ok(dossier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب ملف الموظف: {ex.Message}");
            }
        }

        [HttpPost("calculate-cost")]
        public async Task<IActionResult> CalculateBatchCost([FromBody] BatchCostCalculationRequestDto req)
        {
            try
            {
                var order = await _context.ProductionOrders
                    .AsNoTracking()
                    .Include(o => o.ProductionOrderDetails)
                        .ThenInclude(d => d.Item)
                    .FirstOrDefaultAsync(o => o.Id == req.ProductionOrderId);

                if (order == null) return NotFound("أمر التشغيل غير موجود.");

                var logs = await _context.ProductivityLogs
                    .AsNoTracking()
                    .Where(p => p.ProductionOrderId == req.ProductionOrderId)
                    .ToListAsync();

                decimal totalHours = logs.Sum(l => l.HoursWorked);
                if (totalHours <= 0) totalHours = 8m;

                decimal totalLaborCost = logs.Any(l => l.LaborCost > 0)
                    ? logs.Sum(l => l.LaborCost)
                    : (totalHours * req.LaborCostPerHour);

                decimal totalMaterialsCost = req.ActualRawMaterialsCost > 0
                    ? req.ActualRawMaterialsCost
                    : order.ProductionOrderDetails.Sum(d => (d.ProducedQuantity > 0 ? d.ProducedQuantity : d.PlannedQuantity) * d.Item.BasePrice);

                decimal totalProducedQty = order.ProductionOrderDetails.Sum(d => d.ProducedQuantity > 0 ? d.ProducedQuantity : d.PlannedQuantity);
                decimal totalBatchCost = totalMaterialsCost + totalLaborCost + req.OverheadCost;
                decimal unitCost = totalProducedQty > 0 ? (totalBatchCost / totalProducedQty) : 0;

                var result = new
                {
                    order.Id,
                    order.OrderNumber,
                    ProducedQuantity = totalProducedQty,
                    ActualHoursWorked = totalHours,
                    TotalBatchCost = Math.Round(totalBatchCost, 2),
                    UnitCost = Math.Round(unitCost, 2),
                    CostBreakdown = new
                    {
                        TotalMaterialsCost = Math.Round(totalMaterialsCost, 2),
                        TotalLaborCost = Math.Round(totalLaborCost, 2),
                        OverheadCost = Math.Round(req.OverheadCost, 2)
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في حساب التكلفة: {ex.Message}");
            }
        }
    }
}