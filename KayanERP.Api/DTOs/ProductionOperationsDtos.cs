using System;
using System.Collections.Generic;

namespace KayanERP.Api.DTOs
{
    // ==========================================
    // 1. DTOs إصدار أوامر التشغيل (من ملفك الأصلي)
    // ==========================================
    public class CreateBatchOrderDto
    {
        public int CompanyId { get; set; } = 1;
        public string? OrderNumber { get; set; }
        public DateOnly OrderDate { get; set; }
        public long WarehouseId { get; set; }
        public long DepartmentId { get; set; }
        public string? Notes { get; set; }
        public List<CreateBatchOrderItemDto> Items { get; set; } = new();
    }

    public class CreateBatchOrderItemDto
    {
        public long ItemId { get; set; }
        public long RecipeId { get; set; }
        public decimal PlannedQuantity { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    // ==========================================
    // 2. DTOs التنفيذ الميداني وتقفيل اللوط والهالك (من ملفك الأصلي)
    // ==========================================
    public class CompleteOrderFinalizeDto
    {
        public long OrderId { get; set; }
        public List<CompleteOrderItemDto> Items { get; set; } = new();
    }

    public class CompleteOrderItemDto
    {
        public long DetailId { get; set; }
        public decimal ProducedQuantity { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public decimal NormalScrapQuantity { get; set; } = 0;
        public decimal AbnormalScrapQuantity { get; set; } = 0;
        public string? ScrapReason { get; set; }
    }

    // ==========================================
    // 3. DTOs إدارة الوردية وحضور العمال (من ملفك الأصلي)
    // ==========================================
    public class ShiftProductivityDto
    {
        public long ProductionOrderId { get; set; }
        public string ShiftName { get; set; } = "وردية صباحية";
        public string StartTime { get; set; } = "07:00";
        public string EndTime { get; set; } = "15:00";
        public decimal TotalProducedQuantity { get; set; }
        public List<ShiftWorkerDto> CrewMembers { get; set; } = new();
    }

    public class ShiftWorkerDto
    {
        public long EmployeeId { get; set; }
        public decimal HoursWorked { get; set; } = 8;
        public decimal HourlyRate { get; set; } = 50;
        public string AttendanceStatus { get; set; } = "حاضر";
    }

    // ==========================================
    // 4. DTOs احتساب التكاليف (من ملفك الأصلي)
    // ==========================================
    public class BatchCostCalculationRequestDto
    {
        public long ProductionOrderId { get; set; }
        public decimal ActualRawMaterialsCost { get; set; }
        public decimal LaborCostPerHour { get; set; } = 50;
        public decimal OverheadCost { get; set; } = 0;
    }

    // ==========================================
    // 5. DTOs القوائم المرجعية الموحدة (Lookups للصالات والعمال)
    // ==========================================
    public class ProductionDepartmentLookupDto
    {
        public long Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? DepartmentCode { get; set; }
    }

    public class ProductionWarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? WarehouseCode { get; set; }
    }

    public class ProductionItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public string? Unit { get; set; }
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
    }

    public class ProductionEmployeeLookupDto
    {
        public long Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
    }

    public class ProductionOperationsLookupsDto
    {
        public List<ProductionDepartmentLookupDto> Departments { get; set; } = new();
        public List<ProductionWarehouseLookupDto> Warehouses { get; set; } = new();
        public List<ProductionItemLookupDto> Items { get; set; } = new();
        public List<ProductionEmployeeLookupDto> Employees { get; set; } = new();
    }

    // ==========================================
    // 6. DTOs استعراض أوامر التشغيل المفتوحة
    // ==========================================
    public class ActiveProductionOrderDetailDto
    {
        public long DetailId { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public decimal PlannedQuantity { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal WasteQuantity { get; set; }
        public string? Unit { get; set; }
        public long? RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? BatchNumber { get; set; }
    }

    public class ActiveProductionOrderDto
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<ActiveProductionOrderDetailDto> Details { get; set; } = new();
    }

    // ==========================================
    // 7. DTOs شاشة التنفيذ الميداني الكاملة (Full Execution)
    // ==========================================
    public class OrderExecutionMaterialDto
    {
        public long ComponentItemId { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal RequiredQuantity { get; set; }
        public decimal ActualConsumedQuantity { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class OrderExecutionItemDto
    {
        public long DetailId { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public string? Unit { get; set; }
        public decimal PlannedQuantity { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal NormalScrapQuantity { get; set; }
        public decimal AbnormalScrapQuantity { get; set; }
        public string ScrapReason { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public List<OrderExecutionMaterialDto> Materials { get; set; } = new();
    }

    public class OrderExecutionFullDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public List<OrderExecutionItemDto> Items { get; set; } = new();
    }

    // ==========================================
    // 8. DTOs مؤشرات الأداء، الحضور، وتفاصيل التكلفة
    // ==========================================
    public class DepartmentKpiDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal TotalProduced { get; set; }
        public decimal TotalLaborCost { get; set; }
        public decimal ProductivityPerHour { get; set; }
        public decimal EstimatedWaste { get; set; }
    }

    public class DetailedAttendanceLogDto
    {
        public long Id { get; set; }
        public DateTime LogDate { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string AttendanceStatus { get; set; } = string.Empty;
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal LaborCost { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal ProductivityPerHour { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class EmployeeDossierDto
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string Department { get; set; } = string.Empty;
        public int TotalOrdersExecuted { get; set; }
        public decimal TotalQuantityProduced { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public decimal TotalLaborCost { get; set; }
        public decimal ProductionPerHour { get; set; }
        public decimal TotalWaste { get; set; }
        public string WastePercentage { get; set; } = string.Empty;
        public string Evaluation { get; set; } = string.Empty;
    }

    public class CostBreakdownDto
    {
        public decimal TotalMaterialsCost { get; set; }
        public decimal TotalLaborCost { get; set; }
        public decimal OverheadCost { get; set; }
    }

    public class BatchCostCalculationResultDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal ProducedQuantity { get; set; }
        public decimal ActualHoursWorked { get; set; }
        public decimal TotalBatchCost { get; set; }
        public decimal UnitCost { get; set; }
        public CostBreakdownDto CostBreakdown { get; set; } = new();
    }
}