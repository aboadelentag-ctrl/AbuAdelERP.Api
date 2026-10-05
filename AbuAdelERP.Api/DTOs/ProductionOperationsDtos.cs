using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.DTOs
{
    // ==========================================
    // 1. DTOs إصدار أوامر التشغيل
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
    // 2. DTOs التنفيذ الميداني وتقفيل اللوط والهالك
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
    // 3. DTOs إدارة الوردية وحضور العمال
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
    // 4. DTOs احتساب التكاليف (Costing)
    // ==========================================
    public class BatchCostCalculationRequestDto
    {
        public long ProductionOrderId { get; set; }
        public decimal ActualRawMaterialsCost { get; set; }
        public decimal LaborCostPerHour { get; set; } = 50;
        public decimal OverheadCost { get; set; } = 0;
    }
}