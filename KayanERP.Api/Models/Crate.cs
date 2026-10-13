using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

[Table("crates", Schema = "erp")]
public partial class Crate
{
    [Column("id")]
    public long Id { get; set; }

    [Column("crate_name")]
    public string CrateName { get; set; } = null!; // مثال: سبت بلاستيك مخرم، سبت عجين، عبوة كرتون

    [Column("total_factory_balance")]
    public int TotalFactoryBalance { get; set; } // الرصيد الحالي المتاح داخل المصنع

    public virtual ICollection<CrateTransaction> CrateTransactions { get; set; } = new List<CrateTransaction>();
}