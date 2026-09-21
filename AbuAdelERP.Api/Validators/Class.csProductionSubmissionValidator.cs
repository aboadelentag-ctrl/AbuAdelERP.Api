using FluentValidation;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Validators;

public class ProductionSubmissionValidator : AbstractValidator<ProductionSubmissionDto>
{
    public ProductionSubmissionValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0).WithMessage("معرف الشركة غير صالح، تأكد من اختيار الشركة.");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب تحديد مستودع استلام صالح لإضافة الإنتاج.");

        RuleFor(x => x.ProductItemId)
            .GreaterThan(0).WithMessage("يجب تحديد الصنف المراد إنتاجه بدقة.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("معرف المستخدم مطلوب لتسجيل مسؤولية الإنتاج.");

        RuleFor(x => x.ProducedQuantity)
            .GreaterThan(0).WithMessage("خطأ: كمية الإنتاج يجب أن تكون أكبر من الصفر.");

        RuleFor(x => x.ActualHours)
            .GreaterThan(0).WithMessage("ساعات العمل الفعلية يجب أن تكون أكبر من الصفر لحساب الأجور والتكاليف.");

        RuleFor(x => x.WasteCost)
            .GreaterThanOrEqualTo(0).WithMessage("تكلفة الهالك لا يمكن أن تكون قيمة سالبة.");
    }
}