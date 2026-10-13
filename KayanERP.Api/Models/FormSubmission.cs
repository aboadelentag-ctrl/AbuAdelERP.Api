using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class FormSubmission
{
    public long Id { get; set; }

    public long FormId { get; set; }

    public long? UserId { get; set; }

    public string SubmissionData { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? SubmittedAt { get; set; }

    // تم إضافة علامة الاستفهام هنا ليعرف النظام أن هذا الحقل اختياري عند الاستقبال
    public virtual DynamicForm? Form { get; set; }
}