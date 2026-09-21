using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class DynamicForm
{
    public long Id { get; set; }

    public string FormName { get; set; } = null!;

    public string FormCode { get; set; } = null!;

    public string? Description { get; set; }

    public string FieldsSchema { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();
}
