using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SuppliersController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب كافة الموردين (GET: api/suppliers)
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .AsNoTracking()
                    .OrderBy(s => s.Id)
                    .Select(s => new SupplierResponseDto
                    {
                        Id = s.Id,
                        CompanyId = s.CompanyId,
                        SupplierCode = s.SupplierCode,
                        Code = s.SupplierCode,
                        SupplierName = s.SupplierName,
                        Name = s.SupplierName,
                        Phone = s.Phone,
                        CreditLimit = s.CreditLimit,
                        Active = s.Active,
                        IsApproved = s.IsApproved,
                        QualityStatus = s.IsApproved ? "مورد معتمد" : "تحت التقييم (غير معتمد)",
                        QualityNotes = s.Notes,
                        Notes = s.Notes
                    })
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الموردين: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب مورد محدد (GET: api/suppliers/{id})
        // ==============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplier(long id)
        {
            try
            {
                var s = await _context.Suppliers.FindAsync(id);
                if (s == null) return NotFound("المورد غير موجود.");

                return Ok(new SupplierResponseDto
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    SupplierCode = s.SupplierCode,
                    Code = s.SupplierCode,
                    SupplierName = s.SupplierName,
                    Name = s.SupplierName,
                    Phone = s.Phone,
                    CreditLimit = s.CreditLimit,
                    Active = s.Active,
                    IsApproved = s.IsApproved,
                    QualityStatus = s.IsApproved ? "مورد معتمد" : "تحت التقييم (غير معتمد)",
                    QualityNotes = s.Notes,
                    Notes = s.Notes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. إضافة مورد جديد (POST: api/suppliers)
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierInputDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SupplierName))
            {
                return BadRequest("اسم المورد مطلوب.");
            }

            try
            {
                var companyId = dto.CompanyId > 0 ? dto.CompanyId : ((await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1);
                var code = !string.IsNullOrWhiteSpace(dto.SupplierCode)
                    ? dto.SupplierCode.Trim()
                    : (!string.IsNullOrWhiteSpace(dto.Code) ? dto.Code.Trim() : $"SUP-{(await _context.Suppliers.CountAsync() + 1):D3}");

                var name = !string.IsNullOrWhiteSpace(dto.SupplierName) ? dto.SupplierName.Trim() : dto.Name?.Trim() ?? string.Empty;
                var notes = !string.IsNullOrWhiteSpace(dto.QualityNotes) ? dto.QualityNotes.Trim() : dto.Notes?.Trim();
                bool isApproved = dto.IsApproved || (dto.QualityStatus == "مورد معتمد");

                var supplier = new Supplier
                {
                    CompanyId = companyId,
                    SupplierCode = code,
                    SupplierName = name,
                    Phone = dto.Phone?.Trim(),
                    CreditLimit = dto.CreditLimit,
                    Active = dto.Active,
                    IsApproved = isApproved,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, id = supplier.Id, message = "تم حفظ المورد بنجاح!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ المورد: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. تعديل بيانات مورد (PUT: api/suppliers/{id}) - يحل خطأ فشل الحفظ
        // ==============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(long id, [FromBody] SupplierInputDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null) return NotFound("المورد غير موجود.");

                var name = !string.IsNullOrWhiteSpace(dto.SupplierName) ? dto.SupplierName.Trim() : dto.Name?.Trim();
                if (!string.IsNullOrWhiteSpace(name)) supplier.SupplierName = name;

                var code = !string.IsNullOrWhiteSpace(dto.SupplierCode) ? dto.SupplierCode.Trim() : dto.Code?.Trim();
                if (!string.IsNullOrWhiteSpace(code)) supplier.SupplierCode = code;

                supplier.Phone = dto.Phone?.Trim();
                supplier.CreditLimit = dto.CreditLimit;
                supplier.Active = dto.Active;
                supplier.IsApproved = dto.IsApproved || (dto.QualityStatus == "مورد معتمد");
                supplier.Notes = !string.IsNullOrWhiteSpace(dto.QualityNotes) ? dto.QualityNotes.Trim() : dto.Notes?.Trim();
                supplier.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "تم تحديث بيانات المورد بنجاح!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تحديث المورد: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. مسار الحذف الفعلي (DELETE: api/suppliers/{id}) - يحل خطأ MethodNotAllowed
        // ==============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(long id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.PurchaseOrders)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                {
                    return NotFound("المورد غير موجود بالفعل.");
                }

                // فحص الارتباط بأوامر الشراء لحماية تكامل البيانات
                if (supplier.PurchaseOrders != null && supplier.PurchaseOrders.Any())
                {
                    return BadRequest("لا يمكن حذف المورد نظراً لوجود أوامر شراء مسجلة باسمه في المنظومة. يمكنك إيقاف تعامله بدلاً من حذفه.");
                }

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "تم حذف المورد نهائياً بنجاح!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"تعذر حذف المورد: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // DTOs لدعم التوافق مع أسماء خصائص الواجهة
        // ==============================================================
        public class SupplierInputDto
        {
            public long Id { get; set; }
            public long CompanyId { get; set; } = 1;
            public string? SupplierCode { get; set; }
            public string? Code { get; set; }
            public string? SupplierName { get; set; }
            public string? Name { get; set; }
            public string? Phone { get; set; }
            public decimal CreditLimit { get; set; } = 0;
            public bool Active { get; set; } = true;
            public bool IsApproved { get; set; } = false;
            public string? QualityStatus { get; set; }
            public string? QualityNotes { get; set; }
            public string? Notes { get; set; }
        }

        public class SupplierResponseDto
        {
            public long Id { get; set; }
            public long CompanyId { get; set; }
            public string SupplierCode { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public decimal CreditLimit { get; set; }
            public bool Active { get; set; }
            public bool IsApproved { get; set; }
            public string QualityStatus { get; set; } = string.Empty;
            public string? QualityNotes { get; set; }
            public string? Notes { get; set; }
        }
    }
}