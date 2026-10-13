using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class UnitConversion
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long FromUnitId { get; set; }

    public long ToUnitId { get; set; }

    public decimal ConversionFactor { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Unit FromUnit { get; set; } = null!;

    public virtual Unit ToUnit { get; set; } = null!;
}
