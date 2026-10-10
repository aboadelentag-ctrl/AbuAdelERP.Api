using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FormReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetReportDashboard(
            [FromQuery] long formId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? department)
        {
            if (formId <= 0)
                return BadRequest("يرجى اختيار نموذج صالح للاستعلام.");

            var form = await _context.DynamicForms.FindAsync(formId);
            if (form == null)
                return NotFound("النموذج غير موجود.");

            // 1. جلب سجلات الاستجابات
            var allSubmissions = await _context.FormSubmissions
                .Where(s => s.FormId == formId)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            // 2. فلترة التواريخ بأمان
            var filtered = allSubmissions.AsEnumerable();

            if (fromDate.HasValue)
            {
                var fDate = fromDate.Value.Date;
                filtered = filtered.Where(s => (s.SubmittedAt ?? DateTime.UtcNow).Date >= fDate);
            }

            if (toDate.HasValue)
            {
                var tDate = toDate.Value.Date;
                filtered = filtered.Where(s => (s.SubmittedAt ?? DateTime.UtcNow).Date <= tDate);
            }

            var reportDto = new FormReportDashboardDto
            {
                FormId = form.Id,
                FormName = form.FormName,
                FormCode = form.FormCode
            };

            int totalComplianceChecks = 0;
            int totalPassedChecks = 0;
            double sumQuantities = 0;

            var deptCounts = new Dictionary<string, int>();
            var nonComplianceFrequency = new Dictionary<string, int>();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var sub in filtered)
            {
                Dictionary<string, string>? answers = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(sub.SubmissionData))
                        answers = JsonSerializer.Deserialize<Dictionary<string, string>>(sub.SubmissionData, jsonOptions);
                }
                catch { }

                // استخراج الحقول المحورية بذكاء من الـ JSON
                string subDept = GetAnswerValue(answers, "dept", "department", "القسم", "القسم / الصالة");
                string subItem = GetAnswerValue(answers, "product_name", "item", "الصنف", "الاسم", "الموظف");
                string subReason = GetAnswerValue(answers, "reason", "المسبب", "المخالفة", "violation", "الإجراء التصحيحي");
                string qtyStr = GetAnswerValue(answers, "quantity", "الكمية", "الوزن");

                // فلترة القسم إن حددها المستخدم
                if (!string.IsNullOrWhiteSpace(department) && !subDept.Contains(department.Trim()))
                {
                    continue;
                }

                // تجميع الكميات الرقمية
                if (double.TryParse(qtyStr, out double parsedQty))
                {
                    sumQuantities += parsedQty;
                }

                // إحصائيات الأقسام
                string deptKey = string.IsNullOrWhiteSpace(subDept) ? "غير محدد" : subDept;
                deptCounts[deptKey] = deptCounts.GetValueOrDefault(deptKey) + 1;

                // فحص بنود الفحص وقوائم المطابقة
                if (answers != null)
                {
                    foreach (var kv in answers)
                    {
                        if (kv.Key.EndsWith("_status"))
                        {
                            totalComplianceChecks++;
                            if (kv.Value == "مطابق")
                            {
                                totalPassedChecks++;
                            }
                            else if (kv.Value == "غير مطابق")
                            {
                                string rowPrefix = kv.Key.Replace("_status", "");
                                nonComplianceFrequency[rowPrefix] = nonComplianceFrequency.GetValueOrDefault(rowPrefix) + 1;
                            }
                        }
                    }
                }

                // فحص وجود الوسائط التوثيقية
                bool hasImg = answers != null && answers.ContainsKey("report_attachment_image") && !string.IsNullOrEmpty(answers["report_attachment_image"]);
                bool hasVid = answers != null && answers.ContainsKey("report_attachment_video") && !string.IsNullOrEmpty(answers["report_attachment_video"]);
                bool hasAud = answers != null && answers.ContainsKey("report_attachment_audio") && !string.IsNullOrEmpty(answers["report_attachment_audio"]);

                reportDto.SubmissionsLog.Add(new FormSubmissionSummaryDto
                {
                    Id = sub.Id,
                    SubmittedByName = $"مستخدم #{sub.UserId}",
                    SubmittedAt = sub.SubmittedAt ?? DateTime.UtcNow,
                    Department = subDept,
                    ItemOrName = subItem,
                    Quantity = qtyStr,
                    ReasonOrAction = subReason,
                    HasImage = hasImg,
                    HasVideo = hasVid,
                    HasAudio = hasAud,
                    Status = sub.Status ?? "معتمد",
                    AnswersJson = sub.SubmissionData ?? "{}"
                });
            }

            reportDto.TotalSubmissions = reportDto.SubmissionsLog.Count;
            reportDto.TotalQuantity = Math.Round(sumQuantities, 2);
            reportDto.TotalViolations = totalComplianceChecks - totalPassedChecks;
            reportDto.ComplianceRate = totalComplianceChecks > 0
                ? Math.Round(((double)totalPassedChecks / totalComplianceChecks) * 100, 1)
                : 100.0;

            // توزيع الأقسام مع النسب
            reportDto.DepartmentStats = deptCounts
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new DepartmentStatItem
                {
                    DepartmentName = kv.Key,
                    Count = kv.Value,
                    Percentage = reportDto.TotalSubmissions > 0 ? Math.Round(((double)kv.Value / reportDto.TotalSubmissions) * 100, 1) : 0
                }).ToList();

            // أعلى أسباب الخلل أو بنود عدم المطابقة
            reportDto.TopViolations = nonComplianceFrequency
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .Select(kv => new TopViolationItem { ItemKey = kv.Key, Count = kv.Value })
                .ToList();

            return Ok(reportDto);
        }

        private string GetAnswerValue(Dictionary<string, string>? dict, params string[] keys)
        {
            if (dict == null) return "";
            foreach (var key in keys)
            {
                var match = dict.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase) || kv.Key.Contains(key));
                if (!string.IsNullOrEmpty(match.Value)) return match.Value;
            }
            return "";
        }
    }

    public class FormReportDashboardDto
    {
        public long FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public string FormCode { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
        public double ComplianceRate { get; set; } = 100.0;
        public int TotalViolations { get; set; }
        public double TotalQuantity { get; set; }
        public List<DepartmentStatItem> DepartmentStats { get; set; } = new();
        public List<TopViolationItem> TopViolations { get; set; } = new();
        public List<FormSubmissionSummaryDto> SubmissionsLog { get; set; } = new();
    }

    public class DepartmentStatItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class TopViolationItem
    {
        public string ItemKey { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class FormSubmissionSummaryDto
    {
        public long Id { get; set; }
        public string SubmittedByName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string Department { get; set; } = string.Empty;
        public string ItemOrName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string ReasonOrAction { get; set; } = string.Empty;
        public bool HasImage { get; set; }
        public bool HasVideo { get; set; }
        public bool HasAudio { get; set; }
        public string Status { get; set; } = "معتمد";
        public string AnswersJson { get; set; } = "{}";
    }
}