using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models
{
    /// <summary>
    /// الهيكل الموحد لجميع نماذج المنظومة (Form Studio Definition)
    /// </summary>
    public class FormStudioSchema
    {
        public string FormCode { get; set; } = string.Empty;
        public string FormTitle { get; set; } = string.Empty;
        public string Category { get; set; } = "عام"; // رقابة جودة / إداري / تشغيل وصيانة / مستودعات
        public int Version { get; set; } = 1;
        public List<FormStudioBlock> Blocks { get; set; } = new();
    }

    /// <summary>
    /// الكتلة البنائية للنموذج (Block)
    /// </summary>
    public class FormStudioBlock
    {
        public string Id { get; set; } = "blk_" + Guid.NewGuid().ToString("N")[..6];
        public string BlockType { get; set; } = "fields"; // "instruction" | "fields" | "table" | "approval"
        public string Title { get; set; } = string.Empty;

        // 1. في حال كانت الكتلة نصوصاً وتوجيهات رسمية
        public string InstructionText { get; set; } = string.Empty;

        // 2. في حال كانت الكتلة مجموعة حقول بيانات
        public List<StudioFieldDefinition> Fields { get; set; } = new();

        // 3. في حال كانت الكتلة جدول فحص أو مصفوفة بيانات
        public List<StudioTableColumn> Columns { get; set; } = new();
        public List<StudioTableRow> Rows { get; set; } = new();

        // 4. في حال كانت الكتلة مسار اعتمادات وموافقات إدارية
        public List<StudioApprovalStage> ApprovalStages { get; set; } = new();
    }

    /// <summary>
    /// تعريف الحقل الفردي وخياراته وربطه بالتقارير
    /// </summary>
    public class StudioFieldDefinition
    {
        public string Id { get; set; } = "fld_" + Guid.NewGuid().ToString("N")[..6];
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string FieldType { get; set; } = "text";
        // الأنواع: text, textarea, number, date, manual_select, erp_select

        // مصدر بيانات الـ ERP الحية
        public string? ErpSource { get; set; } // Departments, Employees, Branches, Products, RawMaterials, Equipment

        // الخيارات اليدوية في حال القائمة المنسدلة الثابتة
        public List<string> ManualOptions { get; set; } = new();

        public bool IsRequired { get; set; }
        public bool IsFullWidth { get; set; }

        // ==========================================
        // وسوم ومحركات التقارير الذكية (BI & KPIs)
        // ==========================================
        public bool IsReportFilter { get; set; } = true;  // هل يظهر كفلتر بحث في شاشة التقارير؟
        public bool IsKpiMetric { get; set; }            // هل هو رقم يقاس كـ مؤشر أداء (كمية/تكلفة/حرارة)؟
        public string? ReportDimension { get; set; }    // البعد التحليلي (مثال: DepartmentDimension, EmployeeDimension)
    }

    /// <summary>
    /// تعريف أعمدة الجداول والمصفوفات
    /// </summary>
    public class StudioTableColumn
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ColumnType { get; set; } = "text";
        // الأنواع: text, number, radio, checkbox, select

        public List<string> Options { get; set; } = new(); // مثل: ["مطابق", "غير مطابق"]
        public bool IsComplianceScore { get; set; }       // هل يُحسب هذا العمود في مؤشر نسبة المطابقة العامة % في التقارير؟
    }

    /// <summary>
    /// صفوف وبنود الفحص داخل الجداول
    /// </summary>
    public class StudioTableRow
    {
        public string Id { get; set; } = "row_" + Guid.NewGuid().ToString("N")[..8];
        public string Label { get; set; } = string.Empty;
        public string? ItemCategory { get; set; } // تصنيف البند (نظافة، سلامة مهنية، كفاءة ماكينات) لتقارير التصنيف
    }

    /// <summary>
    /// مراحل الاعتماد والقرارات الإدارية
    /// </summary>
    public class StudioApprovalStage
    {
        public string StageId { get; set; } = "stage_" + Guid.NewGuid().ToString("N")[..6];
        public string RoleTitle { get; set; } = string.Empty; // رئيس القسم، مدير الجودة، المدير العام
        public string Condition { get; set; } = string.Empty;
        public List<string> DecisionOptions { get; set; } = new() { "أوافق", "لا أوافق" };
        public bool HasNotes { get; set; } = true;
        public bool HasSignature { get; set; } = true;
    }
}