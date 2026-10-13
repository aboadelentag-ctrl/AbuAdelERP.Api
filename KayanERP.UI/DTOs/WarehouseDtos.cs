using System;
using System.ComponentModel.DataAnnotations;

namespace KayanERP.UI.DTOs
{
    public class WarehouseDto
    {
        public long Id { get; set; }

        public long? BranchId { get; set; } = 1;

        public string? BranchName { get; set; } = string.Empty;

        [Required(ErrorMessage = "كود المخزن مطلوب.")]
        public string WarehouseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المخزن مطلوب.")]
        public string WarehouseName { get; set; } = string.Empty;

        public string WarehouseType { get; set; } = "خامات";

        public long? ParentWarehouseId { get; set; }

        public string? ParentWarehouseName { get; set; }

        public string? Description { get; set; }

        public bool Active { get; set; } = true;
    }

    public class BranchLookupDto
    {
        public long Id { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }
}