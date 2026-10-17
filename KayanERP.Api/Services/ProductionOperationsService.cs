using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public interface IProductionOperationsService
    {
        Task<ProductionOperationsLookupsDto> GetInitialLookupsAsync();
        Task<List<ActiveProductionOrderDto>> GetActiveOrdersAsync();
        Task<(bool Success, string Message, long OrderId, string OrderNumber)> CreateOrderAsync(CreateBatchOrderDto dto);
        Task<OrderExecutionFullDto?> GetOrderExecutionFullAsync(long orderId);
        Task<bool> FinalizeOrderCompleteAsync(CompleteOrderFinalizeDto dto);
        Task<string> SubmitShiftProductivityAsync(ShiftProductivityDto dto);
        Task<List<DepartmentKpiDto>> GetDepartmentKPIsAsync(DateTime startDate, DateTime endDate, long? departmentId);
        Task<List<DetailedAttendanceLogDto>> GetDetailedAttendanceLogsAsync(DateTime startDate, DateTime endDate, long? departmentId);
        Task<EmployeeDossierDto?> GetEmployeeDossierAsync(long employeeId);
        Task<BatchCostCalculationResultDto?> CalculateBatchCostAsync(BatchCostCalculationRequestDto req);
    }

    public class ProductionOperationsService : IProductionOperationsService
    {
        private readonly AppDbContext _context;

        public ProductionOperationsService(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. الدورة المغلقة: سحب صالات الإنتاج ومخازنها وعمالها
        // ========================================================
        public async Task<ProductionOperationsLookupsDto> GetInitialLookupsAsync()
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
                .Select(d => new ProductionDepartmentLookupDto
                {
                    Id = d.Id,
                    DepartmentName = d.DepartmentName,
                    DepartmentCode = d.DepartmentCode
                })
                .ToListAsync();

            var prodDeptIds = departments.Select(d => d.Id).ToList();

            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Active &&
                           (w.WarehouseName.Contains("صالة")
                         || w.WarehouseName.Contains("تشغيل")
                         || w.WarehouseName.Contains("قسم")
                         || w.WarehouseName.Contains("شرقي")
                         || w.WarehouseName.Contains("جاتوه")
                         || w.WarehouseName.Contains("تورت")
                         || w.WarehouseName.Contains("اسبشيال")))
                .OrderBy(w => w.WarehouseName)
                .Select(w => new ProductionWarehouseLookupDto
                {
                    Id = w.Id,
                    WarehouseName = w.WarehouseName,
                    WarehouseCode = w.WarehouseCode
                })
                .ToListAsync();

            var stockBalancesMap = await _context.StockBalances
                .AsNoTracking()
                .GroupBy(s => s.ItemId)
                .Select(g => new { ItemId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ItemId, x => x.TotalQty);

            var itemsQuery = await _context.Items
                .AsNoTracking()
                .Include(i => i.Department)
                .Where(i => i.Active && (i.ItemType == "منتج تام" || i.ItemType == "نصف مصنع" || i.ItemType == "منتج وسيط" || i.ItemType == "عجين"))
                .ToListAsync();

            var items = itemsQuery.Select(i => new ProductionItemLookupDto
            {
                Id = i.Id,
                ItemCode = i.ItemCode,
                ItemName = i.ItemName,
                ItemType = i.ItemType,
                Unit = i.Unit,
                DepartmentId = i.DepartmentId,
                DepartmentName = i.Department != null ? i.Department.DepartmentName : string.Empty,
                CurrentStock = stockBalancesMap.TryGetValue(i.Id, out var qty) ? qty : 0
            }).ToList();

            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Include(e => e.JobTitle)
                .Where(e => e.Active && e.DepartmentId.HasValue && prodDeptIds.Contains(e.DepartmentId.Value))
                .Select(e => new ProductionEmployeeLookupDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    Name = e.Name,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : "صالة إنتاج",
                    JobTitle = e.JobTitle != null ? e.JobTitle.TitleName : "عامل إنتاج"
                })
                .ToListAsync();

            return new ProductionOperationsLookupsDto
            {
                Departments = departments,
                Warehouses = warehouses,
                Items = items,
                Employees = employees
            };
        }

        // ========================================================
        // 2. استعراض أوامر التشغيل المفتوحة
        // ========================================================
        public async Task<List<ActiveProductionOrderDto>> GetActiveOrdersAsync()
        {
            var orders = await _context.ProductionOrders
                .AsNoTracking()
                .Include(o => o.Warehouse)
                .Include(o => o.ProductionOrderDetails)
                    .ThenInclude(d => d.Item)
                        .ThenInclude(i => i!.Department)
                .Include(o => o.ProductionOrderDetails)
                    .ThenInclude(d => d.Recipe)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return orders.Select(o => new ActiveProductionOrderDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status,
                WarehouseId = o.WarehouseId,
                WarehouseName = o.Warehouse != null ? o.Warehouse.WarehouseName : "مخزن صالة التشغيل",
                DepartmentId = o.ProductionOrderDetails.Select(d => d.Item?.DepartmentId).FirstOrDefault() ?? 0,
                DepartmentName = o.ProductionOrderDetails.Select(d => d.Item?.Department?.DepartmentName).FirstOrDefault() ?? "صالة إنتاج",
                Notes = o.Notes,
                Details = o.ProductionOrderDetails.Select(d => new ActiveProductionOrderDetailDto
                {
                    DetailId = d.Id,
                    ItemId = d.ItemId,
                    ItemCode = d.Item?.ItemCode ?? string.Empty,
                    ItemName = d.Item?.ItemName ?? string.Empty,
                    ItemType = d.Item?.ItemType,
                    PlannedQuantity = d.PlannedQuantity,
                    ProducedQuantity = d.ProducedQuantity,
                    WasteQuantity = d.WasteQuantity,
                    Unit = d.Item?.Unit,
                    RecipeId = d.RecipeId,
                    RecipeName = d.Recipe != null ? d.Recipe.RecipeName : string.Empty,
                    Status = d.Status,
                    BatchNumber = d.Notes
                }).ToList()
            }).ToList();
        }

        // ========================================================
        // 3. إصدار أمر تشغيل صالة جديد
        // ========================================================
        public async Task<(bool Success, string Message, long OrderId, string OrderNumber)> CreateOrderAsync(CreateBatchOrderDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return (false, "يجب تحديد صنف واحد على الأقل في أمر التشغيل.", 0, string.Empty);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = dto.CompanyId > 0 ? dto.CompanyId : 1;
                var orderNumber = !string.IsNullOrWhiteSpace(dto.OrderNumber)
                    ? dto.OrderNumber.Trim()
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

                var itemIds = dto.Items.Select(x => x.ItemId).Distinct().ToList();
                var recipes = await _context.Recipes
                    .AsNoTracking()
                    .Where(r => itemIds.Contains(r.ProductItemId) && r.Active)
                    .ToListAsync();

                foreach (var line in dto.Items)
                {
                    var recipe = recipes.FirstOrDefault(r => r.ProductItemId == line.ItemId);

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

                return (true, "تم إصدار أمر التشغيل بنجاح", order.Id, order.OrderNumber);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ========================================================
        // 4. استدعاء تفاصيل التنفيذ الميداني وشجرة المواد
        // ========================================================
        public async Task<OrderExecutionFullDto?> GetOrderExecutionFullAsync(long orderId)
        {
            var order = await _context.ProductionOrders
                .AsNoTracking()
                .Include(o => o.Warehouse)
                .Include(o => o.ProductionOrderDetails)
                    .ThenInclude(d => d.Item)
                .Include(o => o.ProductionOrderDetails)
                    .ThenInclude(d => d.Recipe)
                        .ThenInclude(r => r!.RecipeLines)
                            .ThenInclude(rl => rl.ComponentItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;

            var items = order.ProductionOrderDetails.Select(d => new OrderExecutionItemDto
            {
                DetailId = d.Id,
                ItemId = d.ItemId,
                ItemCode = d.Item?.ItemCode ?? string.Empty,
                ItemName = d.Item?.ItemName ?? string.Empty,
                ItemType = d.Item?.ItemType,
                Unit = d.Item?.Unit,
                PlannedQuantity = d.PlannedQuantity,
                ProducedQuantity = d.ProducedQuantity > 0 ? d.ProducedQuantity : d.PlannedQuantity,
                NormalScrapQuantity = 0m,
                AbnormalScrapQuantity = 0m,
                ScrapReason = string.Empty,
                Status = d.Status,
                BatchNumber = !string.IsNullOrEmpty(d.Notes) ? d.Notes : $"LOT-{DateTime.UtcNow:yyyyMMdd}-{d.Item?.ItemCode}-{d.Id}",
                ExpiryDate = DateTime.UtcNow.AddDays(Convert.ToDouble(d.Item?.ShelfLifeDays.HasValue == true && d.Item.ShelfLifeDays.Value > 0 ? d.Item.ShelfLifeDays.Value : 4)),
                Materials = d.Recipe != null ? d.Recipe.RecipeLines.Select(rl => new OrderExecutionMaterialDto
                {
                    ComponentItemId = rl.ComponentItemId,
                    ComponentCode = rl.ComponentItem?.ItemCode ?? string.Empty,
                    ComponentName = rl.ComponentItem?.ItemName ?? string.Empty,
                    Unit = rl.Unit,
                    RequiredQuantity = d.Recipe.BatchQuantity > 0 ? (rl.Quantity / d.Recipe.BatchQuantity) * d.PlannedQuantity : (rl.Quantity * d.PlannedQuantity),
                    ActualConsumedQuantity = d.Recipe.BatchQuantity > 0 ? (rl.Quantity / d.Recipe.BatchQuantity) * d.PlannedQuantity : (rl.Quantity * d.PlannedQuantity),
                    UnitCost = rl.ComponentItem?.BasePrice ?? 0
                }).ToList() : new List<OrderExecutionMaterialDto>()
            }).ToList();

            return new OrderExecutionFullDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                WarehouseName = order.Warehouse != null ? order.Warehouse.WarehouseName : "مخزن الصالة",
                Items = items
            };
        }

        // ========================================================
        // 5. تقفيل أمر التشغيل والترصيد المخزني وخصم الخامات بالمعيار (BOM)
        // ========================================================
        public async Task<bool> FinalizeOrderCompleteAsync(CompleteOrderFinalizeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.ProductionOrders
                    .Include(o => o.ProductionOrderDetails)
                        .ThenInclude(d => d.Item)
                    .Include(o => o.ProductionOrderDetails)
                        .ThenInclude(d => d.Recipe)
                            .ThenInclude(r => r!.RecipeLines)
                    .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

                if (order == null) return false;

                var now = DateTime.UtcNow;

                foreach (var itemDto in dto.Items)
                {
                    var detail = order.ProductionOrderDetails.FirstOrDefault(d => d.Id == itemDto.DetailId);
                    if (detail != null)
                    {
                        detail.ProducedQuantity = itemDto.ProducedQuantity;
                        detail.WasteQuantity = itemDto.NormalScrapQuantity + itemDto.AbnormalScrapQuantity;
                        detail.Status = "Completed";
                        detail.Notes = itemDto.BatchNumber;

                        // 1. تسجيل الهالك في سجلات الجودة WasteRecords
                        if (detail.WasteQuantity > 0)
                        {
                            var waste = new WasteRecord
                            {
                                CompanyId = order.CompanyId,
                                ProductionOrderId = order.Id,
                                ItemId = detail.ItemId,
                                WarehouseId = order.WarehouseId,
                                Quantity = detail.WasteQuantity,
                                WasteDate = now,
                                WasteReason = itemDto.AbnormalScrapQuantity > 0
                                    ? $"هالك غير طبيعي ({itemDto.ScrapReason})"
                                    : "هالك تشغيل طبيعي مسموح به",
                                Notes = $"تشغيلة لوط: {itemDto.BatchNumber}"
                            };
                            _context.WasteRecords.Add(waste);
                        }

                        // 2. الترصيد الفعلي للمنتج المصنع في مستودع الصالة (StockBalances)
                        if (detail.ProducedQuantity > 0)
                        {
                            var balance = await _context.StockBalances
                                .FirstOrDefaultAsync(b => b.WarehouseId == order.WarehouseId && b.ItemId == detail.ItemId);

                            if (balance != null)
                            {
                                balance.Quantity += detail.ProducedQuantity;
                                balance.AvailableQuantity = balance.Quantity - balance.ReservedQuantity;
                                balance.LastUpdatedAt = now;
                            }
                            else
                            {
                                _context.StockBalances.Add(new StockBalance
                                {
                                    CompanyId = order.CompanyId,
                                    WarehouseId = order.WarehouseId,
                                    ItemId = detail.ItemId,
                                    Quantity = detail.ProducedQuantity,
                                    AvailableQuantity = detail.ProducedQuantity,
                                    ReservedQuantity = 0,
                                    LastUpdatedAt = now
                                });
                            }

                            // 3. إثبات حركة كارت الصنف الرسمية (PRODUCTION_RECEIPT)
                            decimal unitCost = detail.Item?.BasePrice ?? 0m;
                            _context.StockMovements.Add(new StockMovement
                            {
                                CompanyId = order.CompanyId,
                                WarehouseId = order.WarehouseId,
                                ItemId = detail.ItemId,
                                MovementType = "PRODUCTION_RECEIPT",
                                Quantity = detail.ProducedQuantity,
                                UnitCost = unitCost,
                                ReferenceType = "PRODUCTION_ORDER",
                                ReferenceId = order.Id,
                                Notes = $"استلام إنتاج تام/وسيط صالة التشغيل | لوط: {itemDto.BatchNumber} | أمر تشغيل رقم: {order.OrderNumber}",
                                MovementAt = now,
                                CreatedAt = now
                            });
                        }

                        // 4. خصم الخامات الأولية المستهلكة آلياً بناءً على بطاقة المقادير (BOM)
                        if (detail.ProducedQuantity > 0 && detail.Recipe != null && detail.Recipe.RecipeLines != null && detail.Recipe.RecipeLines.Any())
                        {
                            decimal batchMult = detail.Recipe.BatchQuantity > 0
                                ? (detail.ProducedQuantity / detail.Recipe.BatchQuantity)
                                : detail.ProducedQuantity;

                            foreach (var rl in detail.Recipe.RecipeLines.Where(l => l.Quantity > 0))
                            {
                                decimal consumedQty = Math.Round(rl.Quantity * batchMult * (1m + (rl.WastePercentage / 100m)), 3);
                                if (consumedQty <= 0) continue;

                                var matBalance = await _context.StockBalances
                                    .FirstOrDefaultAsync(b => b.WarehouseId == order.WarehouseId && b.ItemId == rl.ComponentItemId);

                                if (matBalance != null)
                                {
                                    matBalance.Quantity -= consumedQty;
                                    matBalance.AvailableQuantity = matBalance.Quantity - matBalance.ReservedQuantity;
                                    matBalance.LastUpdatedAt = now;
                                }
                                else
                                {
                                    _context.StockBalances.Add(new StockBalance
                                    {
                                        CompanyId = order.CompanyId,
                                        WarehouseId = order.WarehouseId,
                                        ItemId = rl.ComponentItemId,
                                        Quantity = -consumedQty,
                                        AvailableQuantity = -consumedQty,
                                        ReservedQuantity = 0,
                                        LastUpdatedAt = now
                                    });
                                }

                                _context.StockMovements.Add(new StockMovement
                                {
                                    CompanyId = order.CompanyId,
                                    WarehouseId = order.WarehouseId,
                                    ItemId = rl.ComponentItemId,
                                    MovementType = "PRODUCTION_ISSUE",
                                    Quantity = -consumedQty,
                                    UnitCost = 0m,
                                    ReferenceType = "PRODUCTION_ORDER",
                                    ReferenceId = order.Id,
                                    Notes = $"صرف واستهلاك خامات بالمعيار لأمر تشغيل: [{order.OrderNumber}] | لوط: {detail.Notes}",
                                    MovementAt = now,
                                    CreatedAt = now
                                });
                            }
                        }
                    }
                }

                order.Status = "COMPLETED";
                order.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ========================================================
        // 6. ترصيد إنتاجية الوردية وحفظ الحضور وساعات العمل
        // ========================================================
        public async Task<string> SubmitShiftProductivityAsync(ShiftProductivityDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var presentCrew = dto.CrewMembers
                    .Where(c => c.AttendanceStatus == "حاضر" && c.HoursWorked > 0)
                    .ToList();

                if (!presentCrew.Any())
                    throw new InvalidOperationException("لم يتم تحديد أي عمال حاضرين لديهم ساعات عمل فعلية بالوردية.");

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
                    throw new InvalidOperationException("لا يوجد أمر تشغيل مسجل في النظام لربط الوردية به. يرجى إصدار أمر تشغيل أولاً.");
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

                return $"تم اعتماد الوردية بنجاح: ({presentCrew.Count}) حاضر، ({absentCrew.Count}) غائب/إجازة على أمر [{targetOrder.OrderNumber}] ✓";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ========================================================
        // 7. مركز التقارير والـ KPIs
        // ========================================================
        public async Task<List<DepartmentKpiDto>> GetDepartmentKPIsAsync(DateTime startDate, DateTime endDate, long? departmentId)
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

            return await query
                .GroupBy(p => p.Employee != null && p.Employee.Department != null ? p.Employee.Department.DepartmentName : "صالة إنتاج عامة")
                .Select(g => new DepartmentKpiDto
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
        }

        public async Task<List<DetailedAttendanceLogDto>> GetDetailedAttendanceLogsAsync(DateTime startDate, DateTime endDate, long? departmentId)
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

            return await query
                .OrderByDescending(p => p.WorkDate)
                .Select(p => new DetailedAttendanceLogDto
                {
                    Id = p.Id,
                    LogDate = p.WorkDate,
                    EmployeeCode = p.Employee != null ? (p.Employee.EmployeeCode ?? "EMP") : "EMP",
                    EmployeeName = p.Employee != null ? p.Employee.Name : "غير معروف",
                    DepartmentName = p.Employee != null && p.Employee.Department != null ? p.Employee.Department.DepartmentName : "صالة الإنتاج",
                    AttendanceStatus = p.AttendanceStatus,
                    HoursWorked = p.HoursWorked,
                    HourlyRate = p.HourlyRate,
                    LaborCost = p.LaborCost,
                    ProducedQuantity = p.ProducedQuantity,
                    ProductivityPerHour = p.HoursWorked > 0 ? Math.Round(p.ProducedQuantity / p.HoursWorked, 2) : 0,
                    Notes = p.Notes ?? string.Empty
                })
                .ToListAsync();
        }

        public async Task<EmployeeDossierDto?> GetEmployeeDossierAsync(long employeeId)
        {
            var emp = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (emp == null) return null;

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

            return new EmployeeDossierDto
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
        }

        // ========================================================
        // 8. احتساب تكلفة الوجبة وتفجير معايير الـ BOM
        // ========================================================
        public async Task<BatchCostCalculationResultDto?> CalculateBatchCostAsync(BatchCostCalculationRequestDto req)
        {
            var order = await _context.ProductionOrders
                .AsNoTracking()
                .Include(o => o.ProductionOrderDetails)
                    .ThenInclude(d => d.Item)
                .Include(o => o.ProductionOrderDetails)
                    .ThenInclude(d => d.Recipe)
                        .ThenInclude(r => r!.RecipeLines)
                            .ThenInclude(rl => rl.ComponentItem)
                .FirstOrDefaultAsync(o => o.Id == req.ProductionOrderId);

            if (order == null) return null;

            var logs = await _context.ProductivityLogs
                .AsNoTracking()
                .Where(p => p.ProductionOrderId == req.ProductionOrderId)
                .ToListAsync();

            decimal totalHours = logs.Sum(l => l.HoursWorked);
            if (totalHours <= 0) totalHours = 8m;

            decimal totalLaborCost = logs.Any(l => l.LaborCost > 0)
                ? logs.Sum(l => l.LaborCost)
                : (totalHours * req.LaborCostPerHour);

            decimal totalMaterialsCost = 0m;
            if (req.ActualRawMaterialsCost > 0)
            {
                totalMaterialsCost = req.ActualRawMaterialsCost;
            }
            else
            {
                foreach (var d in order.ProductionOrderDetails)
                {
                    decimal producedOrPlanned = d.ProducedQuantity > 0 ? d.ProducedQuantity : d.PlannedQuantity;
                    if (d.Recipe != null && d.Recipe.RecipeLines != null && d.Recipe.RecipeLines.Any())
                    {
                        decimal mult = d.Recipe.BatchQuantity > 0 ? (producedOrPlanned / d.Recipe.BatchQuantity) : producedOrPlanned;
                        decimal recipeCost = d.Recipe.RecipeLines.Sum(rl => rl.Quantity * mult * (rl.ComponentItem != null ? rl.ComponentItem.BasePrice : 0m));
                        totalMaterialsCost += recipeCost;
                    }
                    else
                    {
                        totalMaterialsCost += producedOrPlanned * (d.Item != null ? d.Item.BasePrice : 0m);
                    }
                }
            }

            decimal totalProducedQty = order.ProductionOrderDetails.Sum(d => d.ProducedQuantity > 0 ? d.ProducedQuantity : d.PlannedQuantity);
            decimal totalBatchCost = totalMaterialsCost + totalLaborCost + req.OverheadCost;
            decimal unitCost = totalProducedQty > 0 ? (totalBatchCost / totalProducedQty) : 0;

            return new BatchCostCalculationResultDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ProducedQuantity = totalProducedQty,
                ActualHoursWorked = totalHours,
                TotalBatchCost = Math.Round(totalBatchCost, 2),
                UnitCost = Math.Round(unitCost, 2),
                CostBreakdown = new CostBreakdownDto
                {
                    TotalMaterialsCost = Math.Round(totalMaterialsCost, 2),
                    TotalLaborCost = Math.Round(totalLaborCost, 2),
                    OverheadCost = Math.Round(req.OverheadCost, 2)
                }
            };
        }
    }
}