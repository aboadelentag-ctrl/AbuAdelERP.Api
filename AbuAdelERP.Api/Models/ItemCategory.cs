using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class ItemCategory
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string CategoryCode { get; set; } = null!;

    public string ItemType { get; set; } = null!;

    public string? Description { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company? Company { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
