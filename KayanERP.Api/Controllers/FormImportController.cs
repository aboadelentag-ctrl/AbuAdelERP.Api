using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormImportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FormImportController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // 1. رفع وتحليل ملف Word وتحويله لمسودة مهيكلة في Form Studio
        // =========================================================================
        [HttpPost("upload-and-generate-form")]
        public async Task<IActionResult> UploadAndGenerateForm(IFormFile file, [FromQuery] string? formCode, [FromQuery] string? formTitle)
        {
            if (file == null || file.Length == 0)
                return BadRequest("لم يتم إرفاق أي ملف صالح.");

            try
            {
                string rawFileName = Path.GetFileNameWithoutExtension(file.FileName);
                string extractedTitle = !string.IsNullOrWhiteSpace(formTitle) ? formTitle.Trim() : rawFileName.Trim();
                string generatedCode = !string.IsNullOrWhiteSpace(formCode) ? formCode.Trim() : $"FORM_{DateTime.Now:yyyyMMddHHmmss}";

                var studioSchema = new FormStudioSchema
                {
                    FormTitle = extractedTitle,
                    FormCode = generatedCode,
                    Category = extractedTitle.Contains("جزاء") || extractedTitle.Contains("إداري") ? "إداري ورقابي" : "جودة وتصنيع"
                };

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                if (memoryStream.Length < 4)
                    return BadRequest("الملف تالف أو غير صالح.");

                using var doc = WordprocessingDocument.Open(memoryStream, false);
                var body = doc.MainDocumentPart?.Document.Body;

                if (body != null)
                {
                    ExtractDocumentToStudioDraft(body, studioSchema);
                }

                // حفظ أو تحديث المسودة في قاعدة البيانات (Upsert)
                var existingForm = await _context.DynamicForms
                    .FirstOrDefaultAsync(f =>
                        (!string.IsNullOrWhiteSpace(formCode) && f.FormCode == formCode) ||
                        f.FormName == extractedTitle);

                bool isUpdate = existingForm != null;
                var formEntity = existingForm ?? new DynamicForm();

                if (!isUpdate)
                {
                    formEntity.FormCode = generatedCode;
                    formEntity.CreatedAt = DateTime.UtcNow;
                    _context.DynamicForms.Add(formEntity);
                }

                formEntity.FormName = extractedTitle;
                formEntity.FieldsSchema = JsonSerializer.Serialize(studioSchema, new JsonSerializerOptions { WriteIndented = false });
                formEntity.IsActive = true;

                await _context.SaveChangesAsync();

                string actionText = isUpdate ? "تحديث وتجديد مسودة" : "استيراد وتوليد مسودة";
                return Ok(new
                {
                    success = true,
                    formId = formEntity.Id,
                    formName = formEntity.FormName,
                    blocksCount = studioSchema.Blocks.Count,
                    message = $"تم {actionText} [{extractedTitle}] بنجاح، وأصبح جاهزاً للتخصيص في مصمم النماذج!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استيراد المسودة: {ex.GetBaseException().Message}");
            }
        }

        // =========================================================================
        // 2. جلب القوائم الحية من جداول الـ ERP الفعلية
        // =========================================================================
        [HttpGet("erp-lookups/{source}")]
        public async Task<IActionResult> GetErpLookups(string source)
        {
            try
            {
                switch (source.ToLower())
                {
                    case "departments":
                        var depts = await _context.Departments
                            .AsNoTracking()
                            .OrderBy(d => d.DepartmentCode)
                            .Select(d => d.DepartmentName)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Distinct()
                            .ToListAsync();
                        return Ok(depts);

                    case "products":
                    case "items":
                        var items = await _context.Items
                            .AsNoTracking()
                            .Where(i => i.Active == true)
                            .OrderBy(i => i.ItemName)
                            .Select(i => i.ItemName)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Distinct()
                            .ToListAsync();
                        return Ok(items);

                    case "employees":
                        var emps = await _context.Employees
                            .AsNoTracking()
                            .Select(e => e.EmployeeCode)
                            .Distinct()
                            .ToListAsync();
                        return Ok(emps);

                    default:
                        return Ok(new List<string>());
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ جلب بيانات الـ ERP: {ex.Message}");
            }
        }

        // =========================================================================
        // 3. حذف نموذج محدد
        // =========================================================================
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteForm(long id)
        {
            var form = await _context.DynamicForms.FindAsync(id);
            if (form == null) return NotFound();
            _context.DynamicForms.Remove(form);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // =========================================================================
        // 4. مسح كافة النماذج لتنظيف النظام
        // =========================================================================
        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAllForms()
        {
            var forms = await _context.DynamicForms.ToListAsync();
            _context.DynamicForms.RemoveRange(forms);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // =========================================================================
        // 5. دوال المعالجة المساعدة (Private Helpers)
        // =========================================================================
        private void ExtractDocumentToStudioDraft(Body body, FormStudioSchema schema)
        {
            // أ) التعليمات والتوجيهات العلوية
            var initialParagraphs = body.Elements<Paragraph>()
                .Select(p => CleanWordText(p.InnerText))
                .Where(t => !string.IsNullOrEmpty(t))
                .TakeWhile(t => !t.Contains("الإدارة") && !t.Contains("الاسم :") && !t.Contains("البند"))
                .ToList();

            if (initialParagraphs.Any())
            {
                schema.Blocks.Add(new FormStudioBlock
                {
                    BlockType = "instruction",
                    Title = "توجيهات وتعليمات عامة",
                    InstructionText = string.Join("\n", initialParagraphs)
                });
            }

            // ب) تحليل الجداول وتقسيمها لكتل
            var tables = body.Descendants<Table>().ToList();
            foreach (var table in tables)
            {
                var rows = table.Descendants<TableRow>().ToList();
                if (!rows.Any()) continue;

                string firstRowText = CleanWordText(rows[0].InnerText);

                // جدول فحص وتدقيق (Checklist)
                if (firstRowText.Contains("مطابق") || firstRowText.Contains("غير مطابق") || firstRowText.Contains("التحقق"))
                {
                    var tableBlock = new FormStudioBlock
                    {
                        BlockType = "table",
                        Title = "بنود الفحص والتقييم"
                    };

                    tableBlock.Columns.Add(new StudioTableColumn { Key = "status", Title = "التقييم", ColumnType = "radio", Options = new() { "مطابق", "غير مطابق" }, IsComplianceScore = true });
                    tableBlock.Columns.Add(new StudioTableColumn { Key = "notes", Title = "ملاحظات", ColumnType = "text" });
                    tableBlock.Columns.Add(new StudioTableColumn { Key = "verified", Title = "التحقق", ColumnType = "checkbox" });

                    foreach (var row in rows.Skip(1))
                    {
                        var cells = row.Descendants<TableCell>().ToList();
                        if (!cells.Any()) continue;
                        string label = CleanWordText(cells[0].InnerText);
                        if (label.Length > 2 && !label.Contains("التوقيع") && !label.Contains("اسم مهندس"))
                        {
                            tableBlock.Rows.Add(new StudioTableRow { Label = label, ItemCategory = "عام" });
                        }
                    }

                    if (tableBlock.Rows.Any()) schema.Blocks.Add(tableBlock);
                    continue;
                }

                // جداول البيانات والموافقات الهجينة
                FormStudioBlock? fieldsBlock = null;
                FormStudioBlock? approvalBlock = null;

                foreach (var row in rows)
                {
                    string rowText = CleanWordText(row.InnerText);
                    if (string.IsNullOrWhiteSpace(rowText) || rowText.Length < 2) continue;

                    if (rowText.Contains("أوافق") || rowText.Contains("قرار") || rowText.Contains("تظلم"))
                    {
                        if (approvalBlock == null)
                        {
                            approvalBlock = new FormStudioBlock { BlockType = "approval", Title = "مسار الموافقات والاعتمادات الرسمية" };
                            schema.Blocks.Add(approvalBlock);
                        }
                        AddApprovalStageFromRow(rowText, approvalBlock);
                    }
                    else
                    {
                        if (fieldsBlock == null)
                        {
                            fieldsBlock = new FormStudioBlock { BlockType = "fields", Title = "البيانات الأساسية" };
                            schema.Blocks.Add(fieldsBlock);
                        }
                        AddFieldsFromRow(row, fieldsBlock);
                    }
                }
            }

            // ج) الملاحظات والإجراءات في ذيل المستند
            var trailing = body.Elements<Paragraph>()
                .Select(p => CleanWordText(p.InnerText))
                .Where(t => t.StartsWith("فى حالة") || t.StartsWith("ملاحظة"))
                .ToList();

            if (trailing.Any())
            {
                schema.Blocks.Add(new FormStudioBlock
                {
                    BlockType = "instruction",
                    Title = "ضوابط وإجراءات التنفيذ",
                    InstructionText = string.Join("\n", trailing)
                });
            }
        }

        private void AddFieldsFromRow(TableRow row, FormStudioBlock block)
        {
            var cells = row.Descendants<TableCell>().ToList();
            foreach (var cell in cells)
            {
                string raw = CleanWordText(cell.InnerText);
                if (string.IsNullOrWhiteSpace(raw) || raw.Length < 2) continue;

                var parts = raw.Split(new[] { ':', '：' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string label = part.Trim();
                    if (string.IsNullOrEmpty(label) || label.Length < 2) continue;

                    if (label.Contains("الإدارة") || label.Contains("القسم"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "dept", Label = label, FieldType = "erp_select", ErpSource = "Departments", IsRequired = true });
                    }
                    else if (label.Contains("الاسم") && !label.Contains("مراقب"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "emp_name", Label = label, FieldType = "erp_select", ErpSource = "Employees", IsRequired = true });
                    }
                    else if (label.Contains("الوظيفة"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "job_title", Label = label, FieldType = "text" });
                    }
                    else if (label.Contains("الصنف") || label.Contains("المنتج"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "product_name", Label = label, FieldType = "erp_select", ErpSource = "Products", IsRequired = true });
                    }
                    else if (label.Contains("الكمية") || label.Contains("الوزن"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "quantity", Label = label, FieldType = "number", IsRequired = true });
                    }
                    else if (label.Contains("المخالفة"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "violation", Label = label, FieldType = "textarea", IsRequired = true });
                    }
                    else if (label.Contains("العقوبة"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "penalty", Label = label, FieldType = "textarea", IsRequired = true });
                    }
                    else if (label.Contains("تاريخ"))
                    {
                        block.Fields.Add(new StudioFieldDefinition { Key = "action_date", Label = label, FieldType = "date" });
                    }
                }
            }
        }

        private void AddApprovalStageFromRow(string rowText, FormStudioBlock block)
        {
            string role = rowText.Contains("الرئيس المباشر") ? "قرار الرئيس المباشر" :
                          rowText.Contains("الموارد البشرية") ? "قرار مدير الموارد البشرية" : "قرار المدير العام";

            if (!block.ApprovalStages.Any(s => s.RoleTitle == role))
            {
                block.ApprovalStages.Add(new StudioApprovalStage
                {
                    RoleTitle = role,
                    Condition = rowText.Contains("فى حالة") ? rowText.Substring(rowText.IndexOf("فى حالة")) : "",
                    DecisionOptions = new() { "أوافق على توقيع العقوبة", "لا أوافق" }
                });
            }
        }

        private string CleanWordText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.Replace("-", "").Replace("_", "").Replace(".", "").Trim();
        }
    }
}