using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class Role
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string RoleName { get; set; } = null!;

    public string RoleCode { get; set; } = null!;

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
