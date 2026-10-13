using System.Collections.Generic;

namespace KayanERP.Api.Models
{
    public static class AppPermissions
    {
        public static readonly List<PermissionGroupDto> Catalog = new()
        {
            new PermissionGroupDto
            {
                GroupCode = "MES",
                GroupName = "🏭 قطاع العمليات الصناعية والتصنيع (MES)",
                Permissions = new List<PermissionItemDto>
                {
                    new("Production.Orders.View", "معاينة أوامر التشغيل والصالات", "استعراض خطط الإنتاج وأوامر التصنيع النشطة"),
                    new("Production.Orders.Create", "زر إصدار أمر تشغيل جديد", "فتح تشغيلة صناعية وتحديد خط الإنتاج والمخرجات"),
                    new("Production.Orders.Edit", "زر تعديل أمر التشغيل", "تعديل الكميات المستهدفة أو الصالة المعنية"),
                    new("Production.Execute.Start", "زر بدء الخلط وصرف الخامات التلقائي", "خصم المواد الخام معيارياً من مستودع الصالة"),
                    new("Production.Waste.Record", "زر تسجيل الهالك والانحرافات", "توثيق نسب الهدر الفعلية أثناء الصب والخبز"),
                    new("Production.Cost.Approve", "زر اعتماد ميزان تكاليف التشغيلة", "صلاحية مالية حساسة لحساب التكلفة الفعلية للوجبة"),
                    new("Production.FGTransfer.Submit", "زر تسليم وترحيل المنتج التام", "تصدير المنتج النهائي بعد اكتمال الصب والتغليف"),
                    new("Production.Orders.Print", "زر طباعة أوامر التشغيل والبطاقات", "توليد كروت المتابعة الميدانية للصالات")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "BOM",
                GroupName = "📖 وصفات التصنيع والخلطات المعيارية (BOM)",
                Permissions = new List<PermissionItemDto>
                {
                    new("BOM.View", "استعراض دليل الوصفات", "مشاهدة قائمة وصفات المنتجات والمقادير"),
                    new("BOM.Create", "زر إنشاء وصفة تصنيع معيارية جديدة", "إضافة تركيبة خلط جديدة ونسب المكونات"),
                    new("BOM.Edit", "زر تعديل نسب ومكونات الوصفة", "تحديث المقادير وأوزان الخامات المعيارية"),
                    new("BOM.Cost.View", "كشف أسرار وتكاليف خلطة الوصفة", "الاطلاع على القيمة المالية لخامات الوصفة"),
                    new("BOM.Delete", "زر حذف / تعطيل الوصفة", "إيقاف العمل بالخلطة في صالات الإنتاج")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "SCM",
                GroupName = "🚚 قطاع المخازن والأرصدة واللوجستيات",
                Permissions = new List<PermissionItemDto>
                {
                    new("Warehouse.Balances.View", "استعراض أرصدة المخازن اللحظية", "الاطلاع على الكميات المتوفرة بالمستودعات والصالات"),
                    new("Warehouse.StockCard.View", "فحص كارت الصنف ومسار اللوطات", "تتبع التاريخ الحركي والتراكمي وتواريخ الصلاحية"),
                    new("Warehouse.Receipt.Inspect", "زر فحص واستلام الخامات الموردة (GMP)", "اعتماد دخول الشحنات واستلام الفحص المبدئي"),
                    new("Warehouse.Adjustment.Create", "زر تحرير تسوية جردية (عجز/زيادة)", "إدخال فروقات الجرد الفعلي للمخازن"),
                    new("Warehouse.Adjustment.Approve", "زر اعتماد وترحيل التسوية الجردية", "التأثير الفعلي على القيمة المالية للمخزون"),
                    new("Warehouse.Barcode.Print", "زر طباعة ملصقات الباركود واللوط", "توليد باركود الصناديق والطرود المخزنية"),
                    new("Warehouse.Export.Excel", "زر تصدير أرصدة المخازن Excel", "استخراج تقارير الجرد والمستودعات خارجياً")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "LOGISTICS",
                GroupName = "🏢 إمداد الفروع وشحن البضائع ولجنة النزاع",
                Permissions = new List<PermissionItemDto>
                {
                    new("Logistics.Transfers.View", "استعراض شحنات وتحويلات الفروع", "متابعة مسار بضاعة الفروع الصادرة والواردة"),
                    new("Logistics.Transfers.Create", "زر إنشاء إذن شحن وإمداد فرع", "حجز البضاعة من المستودع المركزي وتوجيهها"),
                    new("Logistics.Transfers.Dispatch", "زر توجيه السائق واعتماد خروج الشحنة", "خصم الرصيد من المستودع وبدء خط سير النقل"),
                    new("Logistics.Transfers.Receive", "زر تأكيد استلام عهدة الفرع", "تأكيد مدير الفرع للكميات وترصيد مخزن المعرض"),
                    new("Logistics.Transfers.DisputeReport", "زر إثبات عجز / كسر / تلفيات شحنة", "فتح محضر نزاع رسمي بين السائق والفرع"),
                    new("Logistics.Transfers.DisputeResolve", "زر اعتماد قرار لجنة فض النزاعات", "تحميل العجز على السائق أو تسوية الهالك المعتمد")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "SALES",
                GroupName = "🧾 المبيعات ونقاط البيع والفواتير اليومية",
                Permissions = new List<PermissionItemDto>
                {
                    new("Sales.Invoices.View", "عرض سجل فواتير المبيعات", "استعراض فواتير العملاء وإيرادات المنافذ"),
                    new("Sales.Invoices.Create", "زر تحرير فاتورة مبيعات جديدة", "إصدار فاتورة وخصم المنتجات من رصيد المنفذ"),
                    new("Sales.Invoices.OverridePrice", "تعديل سعر البيع أو منح خصم استثنائي", "تجاوز السعر المعياري المعتمد للصنف"),
                    new("Sales.Invoices.Post", "زر ترحيل الفاتورة مالياً", "توليد القيود المحاسبية التلقائية وإقفال الحساب"),
                    new("Sales.Invoices.Cancel", "زر إلغاء / حذف فاتورة مبيعات مرحلة", "إلغاء أثر الفاتورة ورد البضاعة للمخزن"),
                    new("Sales.Return.Create", "زر إصدار مرتجع مبيعات", "معالجة بضاعة العميل المرتجعة للفرع"),
                    new("Sales.Invoices.Print", "زر طباعة الفاتورة والإيصال", "طباعة الفاتورة الضريبية وإيصالات الدفع")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "CRM",
                GroupName = "👥 العملاء والتجار وحسابات المنافذ",
                Permissions = new List<PermissionItemDto>
                {
                    new("Customers.View", "استعراض سجل العملاء والتجار", "الاطلاع على بيانات العملاء وأرصدتهم"),
                    new("Customers.Create", "زر إضافة عميل / منفذ جديد", "تسجيل بيانات تاجر أو عميل جديد"),
                    new("Customers.Edit", "زر تعديل بيانات وسقف ائتمان العميل", "تحديد الحد الأقصى للمديونية وشروط الدفع"),
                    new("Customers.Block", "زر حظر / إيقاف التعامل مع العميل", "منع إصدار فواتير آجلة للعميل"),
                    new("Customers.Statement.Export", "زر تصدير وطباعة كشف حساب العميل", "استخراج مديونيات وحركات العميل المالية")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "FINANCE",
                GroupName = "💵 الإدارة المالية والخزائن وشجرة الحسابات",
                Permissions = new List<PermissionItemDto>
                {
                    new("Finance.COA.View", "استعراض شجرة الحسابات ودليل المركز المالي", "الاطلاع على بنود الأصول والخصوم والمصروفات"),
                    new("Finance.COA.Manage", "زر إضافة / تعديل حساب في الشجرة", "تعديل بنود وهيكل الحسابات المحاسبية"),
                    new("Finance.Treasury.ReceiptCreate", "زر إنشاء سند قبض نقدية / شيكات", "تحرير استلام مبالغ نقدية أو دفعات عملاء"),
                    new("Finance.Treasury.PaymentCreate", "زر إنشاء سند صرف نقدية / مصروفات", "صرف مستحقات أو غاز أو نثريات تشغيل"),
                    new("Finance.Treasury.Approve", "زر اعتماد وترحيل سندات الخزينة", "التأثير الفعلي على رصيد الخزائن وحسابات البنوك"),
                    new("Finance.Journal.Create", "زر إنشاء وتمرير قيد يومية يدوي", "تسجيل القيود المحاسبية والتسويات"),
                    new("Finance.Journal.Post", "زر ترحيل القيود لدفتر الأستاذ العام", "إقفال القيود بشكل نهائي غير قابل للتعديل"),
                    new("Finance.Reports.Statements", "معاينة ميزان المراجعة والأرباح والخسائر", "استعراض القوائم والتقارير المالية الختامية")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "PROCUREMENT",
                GroupName = "📦 المشتريات والموردين وفواتير التوريد",
                Permissions = new List<PermissionItemDto>
                {
                    new("Procurement.Suppliers.Manage", "إدارة سجل الموردين المعتمدين", "إضافة وتعديل بيانات شركات التوريد والأسعار"),
                    new("Procurement.PO.Create", "زر إنشاء أمر شراء (PO)", "إصدار طلب توريد خامات رسمي للمورد"),
                    new("Procurement.PO.Approve", "زر اعتماد أمر الشراء من الإدارة", "الموافقة الرسمية والمالية على أمر التوريد"),
                    new("Procurement.Invoice.Create", "زر تسجيل فاتورة شراء وربطها بالاستلام", "إثبات الفاتورة ومطابقتها مع محضر الاستلام"),
                    new("Procurement.Invoice.Post", "زر اعتماد وترحيل فاتورة المورد", "التأثير على حساب المورد واستحقاقه المالي"),
                    new("Procurement.Quality.Assess", "زر تقييم جودة المورد والشهادات (COA)", "اعتماد نتائج التحاليل المخبرية للخامات")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "QUALITY",
                GroupName = "🔬 منظومة الجودة والمعامل وسلامة الغذاء (HACCP)",
                Permissions = new List<PermissionItemDto>
                {
                    new("Quality.Forms.Design", "زر تصميم وتعديل نماذج الفحص الرقمية", "بناء وتعديل استمارات النظافة والتفتيش"),
                    new("Quality.Forms.Fill", "زر تعبئة محاضر الفحص الميداني والمعملي", "تسجيل درجات الحرارة، الـ pH، والنظافة"),
                    new("Quality.Batch.Release", "زر الإفراج الصحي والنوعي عن التشغيلات", "الموافقة الرسمية على صلاحية المنتج للبيع والتوزيع"),
                    new("Quality.NonConformity.Flag", "زر تسجيل حالة عدم مطابقة وحظر الدفعة", "حجر التشغيلة المخالفة ومنع خروجها")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "EMERGENCY",
                GroupName = "🚨 منظومة الطوارئ والأعطال (SOS Hub)",
                Permissions = new List<PermissionItemDto>
                {
                    new("Emergency.Alert.Create", "زر إطلاق بلاغ عطل أو طوارئ SOS", "توجيه إشعار فوري لفرق الصيانة والتشغيل"),
                    new("Emergency.Alert.Assign", "زر توجيه البلاغ وتكليف الفني المختص", "تحديد المسؤول وقطع الغيار المطلوبة للإصلاح"),
                    new("Emergency.Alert.Close", "زر اعتماد إغلاق البلاغ واكتمال الإصلاح", "إقفال التذكرة وتوثيق زمن الاستجابة SLA")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "HR",
                GroupName = "👥 الموارد البشرية والهيكل الإداري",
                Permissions = new List<PermissionItemDto>
                {
                    new("HR.Employees.View", "استعراض سجل العاملين والملفات", "الاطلاع على بيانات الموظفين والورديات"),
                    new("HR.Employees.Manage", "زر إضافة / تعديل بيانات موظف", "تعديل المسميات والرواتب وصالات العمل"),
                    new("HR.Shifts.Schedule", "زر توزيع وجدولة ورديات العمل اليومية", "تسكين العمال في الورديات الصباحية والمسائية")
                }
            },
            new PermissionGroupDto
            {
                GroupCode = "ADMIN",
                GroupName = "🛡️ إدارة المنظومة وأمن الصلاحيات (RBAC)",
                Permissions = new List<PermissionItemDto>
                {
                    new("Admin.Roles.Create", "زر إنشاء مصفوفة ودور وظيفي جديد", "إضافة رتب جديدة وتحديد مصفوفة صلاحياتها"),
                    new("Admin.Roles.Manage", "زر حفظ وتعديل صلاحيات الأدوار والأزرار", "منح وسحب الصلاحيات التفصيلية للأدوار"),
                    new("Admin.Users.Create", "زر إضافة مستخدم وربطه بملف عامل", "توليد حسابات للموظفين مع كلمات المرور"),
                    new("Admin.Users.Manage", "زر تفعيل / تعطيل حسابات المستخدمين", "إيقاف دخول الموظف فوراً عند الحاجة"),
                    new("Admin.Users.ResetPassword", "زر إعادة تعيين كلمة المرور للمستخدم", "تغيير وتحديث كلمات المرور"),
                    new("Admin.Users.CustomPermissions", "زر تخصيص صلاحيات استثنائية للمستخدم", "منح أزرار إضافية محددة تتجاوز رتبته")
                }
            }
        };
    }

    public class PermissionGroupDto
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public PermissionItemDto(string code, string title, string description)
        {
            Code = code;
            Title = title;
            Description = description;
        }
    }
}