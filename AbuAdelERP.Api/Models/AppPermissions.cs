using System.Collections.Generic;

namespace AbuAdelERP.Api.Models
{
    public class PermissionItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public static class AppPermissions
    {
        public static List<PermissionItem> GetAllPermissions()
        {
            return new List<PermissionItem>
            {
                // ================= 1. إدارة المستخدمين والأمان =================
                new() { Code = "Users.View", Name = "عرض شاشة المستخدمين", Module = "🛡️ المستخدمين والأمان", Description = "الوصول لشاشة إدارة المستخدمين والصلاحيات" },
                new() { Code = "Users.Create", Name = "زر إضافة مستخدم جديد", Module = "🛡️ المستخدمين والأمان", Description = "صلاحية إنشاء حساب جديد وتعيين كلمة المرور" },
                new() { Code = "Users.Edit", Name = "زر تعديل بيانات المستخدم", Module = "🛡️ المستخدمين والأمان", Description = "تعديل بيانات الحساب والقسم والدور" },
                new() { Code = "Users.Permissions", Name = "زر مصفوفة الصلاحيات", Module = "🛡️ المستخدمين والأمان", Description = "فتح شاشة توزيع وتعديل صلاحيات الأزرار للمستخدم" },

                // ================= 2. غرفة عمليات الطوارئ (SOS) =================
                new() { Code = "Emergency.View", Name = "عرض شاشة المهام الطارئة", Module = "🚨 غرفة عمليات الطوارئ (SOS)", Description = "استعراض سجل البلاغات والأعطال الحية" },
                new() { Code = "Emergency.Create", Name = "زر إطلاق بلاغ طارئ (SOS)", Module = "🚨 غرفة عمليات الطوارئ (SOS)", Description = "صلاحية استدعاء الصيانة أو النظافة أو الجودة" },
                new() { Code = "Emergency.Delegate", Name = "زر تكليف وإحالة فني", Module = "🚨 غرفة عمليات الطوارئ (SOS)", Description = "توجيه وتكليف فني محدد بمباشرة البلاغ" },
                new() { Code = "Emergency.Respond", Name = "زر بدء التنفيذ الميداني", Module = "🚨 غرفة عمليات الطوارئ (SOS)", Description = "تسجيل بدء الاستجابة الميدانية للأعطال" },
                new() { Code = "Emergency.Close", Name = "زر إغلاق البلاغ بالدليل", Module = "🚨 غرفة عمليات الطوارئ (SOS)", Description = "رفع صور وفيديو إثبات الحل واعتماد الإغلاق" },
                new() { Code = "Emergency.Reports", Name = "شاشة تقارير مؤشرات الطوارئ (SLA)", Module = "🚨 غرفة عمليات الطوارئ (SOS)", Description = "تحليل مؤشرات MTTR وسرعة الاستجابة والطباعة" },

                // ================= 3. الهيكل الإداري والمؤسسي =================
                new() { Code = "Companies.View", Name = "عرض شاشة إدارة الشركات", Module = "🏢 الهيكل المؤسسي والعمال", Description = "استعراض بيانات الشركات والفروع" },
                new() { Code = "Companies.Manage", Name = "إدارة وتعديل الشركات", Module = "🏢 الهيكل المؤسسي والعمال", Description = "إضافة أو تعديل بيانات الشركات والفروع" },
                new() { Code = "Departments.View", Name = "عرض شاشة إدارة الأقسام والصالات", Module = "🏢 الهيكل المؤسسي والعمال", Description = "استعراض هيكل الإدارات وصالات الإنتاج" },
                new() { Code = "Departments.Manage", Name = "إدارة وتعديل الأقسام", Module = "🏢 الهيكل المؤسسي والعمال", Description = "إضافة وتعديل صالات الإنتاج والأقسام والربط" },
                new() { Code = "Employees.View", Name = "عرض شاشة العمال والورديات", Module = "🏢 الهيكل المؤسسي والعمال", Description = "استعراض سجل الموظفين وتوزيع الورديات" },
                new() { Code = "Employees.Manage", Name = "إدارة العمال والورديات", Module = "🏢 الهيكل المؤسسي والعمال", Description = "إضافة عمال وتوزيعهم على خطوط الإنتاج والورديات" },

                // ================= 4. دليل الأصناف والمواد =================
                new() { Code = "Items.View", Name = "عرض مركز إدارة الأصناف الشامل", Module = "📦 دليل الأصناف والمواد", Description = "استعراض دليل الخامات والمنتجات التامة والتعبئة" },
                new() { Code = "Items.Create", Name = "زر إضافة صنف جديد", Module = "📦 دليل الأصناف والمواد", Description = "تسجيل خامة أو منتج جديد وتحديد الوحدات" },
                new() { Code = "Items.Edit", Name = "زر تعديل بيانات الصنف", Module = "📦 دليل الأصناف والمواد", Description = "تعديل مواصفات الصنف وحدود الأمان والأسعار" },
                new() { Code = "Categories.View", Name = "عرض شاشة تصنيفات الأصناف", Module = "📦 دليل الأصناف والمواد", Description = "استعراض شجرة مجموعات الأصناف" },
                new() { Code = "Categories.Manage", Name = "إدارة وتعديل التصنيفات", Module = "📦 دليل الأصناف والمواد", Description = "إضافة وتعديل مجموعات الأصناف" },

                // ================= 5. المشتريات والموردين =================
                new() { Code = "Suppliers.View", Name = "عرض شاشة إدارة الموردين", Module = "🛒 المشتريات والموردين", Description = "استعراض دليل موردي الخامات ومواد التعبئة" },
                new() { Code = "Suppliers.Manage", Name = "إدارة وتعديل الموردين", Module = "🛒 المشتريات والموردين", Description = "إضافة مورد جديد وتعديل بيانات الاتصال والشروط" },
                new() { Code = "SupplierPrices.View", Name = "عرض قوائم أسعار التوريد", Module = "🛒 المشتريات والموردين", Description = "متابعة أسعار الخامات المتفق عليها مع الموردين" },
                new() { Code = "PurchaseOrders.View", Name = "عرض أوامر الشراء (PO)", Module = "🛒 المشتريات والموردين", Description = "استعراض طلبات وأوامر الشراء الصادرة" },
                new() { Code = "PurchaseOrders.Create", Name = "زر إنشاء أمر شراء جديد", Module = "🛒 المشتريات والموردين", Description = "إصدار أمر توريد خامات جديد" },
                new() { Code = "PurchaseInvoices.View", Name = "عرض فواتير الشراء والتسعير", Module = "🛒 المشتريات والموردين", Description = "مراجعة فواتير التوريد وتكاليف الشراء" },
                new() { Code = "SupplierQuality.View", Name = "عرض شاشة تقييم جودة الموردين", Module = "🛒 المشتريات والموردين", Description = "متابعة درجات تقييم الموردين ونسب الرفض" },

                // ================= 6. المخازن واللوجستيات واللوطات =================
                new() { Code = "MaterialReceipt.View", Name = "عرض شاشة فحص واستلام الخامات", Module = "🏭 المخازن واللوجستيات", Description = "استعراض شحنات الخامات الواردة للمستودع" },
                new() { Code = "MaterialReceipt.Create", Name = "زر تسجيل استلام وفحص خامات", Module = "🏭 المخازن واللوجستيات", Description = "إدخال شحنة واردة وتسجيل رقم اللوط وتاريخ الصلاحية" },
                new() { Code = "Warehouse.View", Name = "عرض شاشة إدارة المخازن", Module = "🏭 المخازن واللوجستيات", Description = "استعراض المستودعات وأماكن التخزين" },
                new() { Code = "StockCard.View", Name = "عرض كارت الصنف وتتبع اللوطات", Module = "🏭 المخازن واللوجستيات", Description = "تتبع حركة أي خامة برقم اللوط وتواريخ الصلاحية FEFO" },
                new() { Code = "StockBalances.View", Name = "عرض شاشة أرصدة المخازن اللحظية", Module = "🏭 المخازن واللوجستيات", Description = "متابعة الجرد الفعلي الفوري في المخازن والصالات" },
                new() { Code = "StockTransfers.View", Name = "عرض شاشة التحويلات بين المخازن", Module = "🏭 المخازن واللوجستيات", Description = "استعراض طلبات نقل البضائع بين الفروع والمستودعات" },
                new() { Code = "StockTransfers.Create", Name = "زر إنشاء تحويل مخزني", Module = "🏭 المخازن واللوجستيات", Description = "إصدار إذن نقل بضاعة بين مستودعين" },
                new() { Code = "StockTransfers.Receive", Name = "زر اعتماد استلام التحويل", Module = "🏭 المخازن واللوجستيات", Description = "تأكيد استلام الكميات المحولة ومنع العجز" },

                // ================= 7. عهد الأصول والصواني والأسبات =================
                new() { Code = "AssetTracking.View", Name = "عرض شاشة تتبع الأصول والعهد (الأسبات والصواني)", Module = "🧺 عهد الأصول والأسبات والصواني", Description = "الوصول لشاشة مراقبة الأسبات والصواني والتروليات" },
                new() { Code = "AssetTracking.CreateMovement", Name = "زر إصدار إذن حركة عهدة (صرف / استلام)", Module = "🧺 عهد الأصول والأسبات والصواني", Description = "صرف أو استلام أسبات وصواني للصالات أو المعارض أو العملاء" },
                new() { Code = "AssetTracking.Approve", Name = "زر اعتماد وتسجيل الحركة وتحديث الأرصدة", Module = "🧺 عهد الأصول والأسبات والصواني", Description = "الترحيل النهائي وتعديل ميزان العهدة فوراً" },

                // ================= 8. الإنتاج والتكاليف وبطاقات الـ BOM =================
                new() { Code = "BOM.View", Name = "عرض شاشة وصفات التصنيع المعيارية (BOM)", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "استعراض مقادير ونسب وتراكيب الأصناف والحلويات" },
                new() { Code = "BOM.Manage", Name = "زر إنشاء وتعديل وصفات التصنيع (BOM)", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "تعديل نسب مكونات العجين والحشو والهالك المعياري" },
                new() { Code = "MaterialRequisition.View", Name = "عرض شاشة طلب وصرف خامات الصالة", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "متابعة طلبات الخامات المحولة لصالات التشغيل" },
                new() { Code = "MaterialRequisition.Create", Name = "زر إصدار طلب صرف خامات", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "طلب صرف دقيق، سكر، زبدة، مكسرات... من المخزن للصالة" },
                new() { Code = "MaterialRequisition.Approve", Name = "زر اعتماد صرف الخامات من المخزن", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "موافقة أمين المخزن وخصم الكميات من الرصيد" },
                new() { Code = "ProductionOrders.View", Name = "عرض غرفة عمليات الإنتاج والتكاليف", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "الاطلاع على أوامر التشغيل وحساب تكلفة الوجبات" },
                new() { Code = "ProductionOrders.Create", Name = "زر إصدار أمر إنتاج وتشغيل جديد", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "إطلاق خطة تصنيع تشغيلة معينة في الصالة" },
                new() { Code = "ProductionOrders.Start", Name = "زر بدء تشغيل أمر الإنتاج", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "تأكيد بدء الخبز والعجن الفعلي داخل الصالة" },
                new() { Code = "ProductionOrders.Close", Name = "زر إغلاق أمر التشغيل واستلام التام", Module = "🍰 الإنتاج والتصنيع والـ BOM", Description = "وزن المنتج التام وحساب الهالك وترحيل التكلفة" },

                // ================= 9. الجودة وسلامة الغذاء (GMP / ISO 22000) =================
                new() { Code = "Quality.InspectBatch", Name = "زر الفحص المعملي للتشغيلات والخامات", Module = "🔬 توكيد الجودة والسلامة", Description = "تسجيل فحص العينات ومطابقتها للمواصفات القياسية" },
                new() { Code = "Quality.IssueCAPA", Name = "زر إصدار إجراء تصحيحي (CAPA)", Module = "🔬 توكيد الجودة والسلامة", Description = "إصدار مخالفة جودة ونظافة وتكليف الإدارات بحلها" },
                new() { Code = "Quality.Approve", Name = "زر اعتماد إجازة الدفعة والتشغيلة", Module = "🔬 توكيد الجودة والسلامة", Description = "السماح بتوزيع المنتج التام بعد التأكد من سلامته" }
            };
        }
    }
}