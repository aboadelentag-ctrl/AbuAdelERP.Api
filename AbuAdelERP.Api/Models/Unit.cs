using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class Unit
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string UnitName { get; set; } = null!;

    public string UnitCode { get; set; } = null!;

    public bool Active { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<UnitConversion> UnitConversionFromUnits { get; set; } = new List<UnitConversion>();

    public virtual ICollection<UnitConversion> UnitConversionToUnits { get; set; } = new List<UnitConversion>();
}
