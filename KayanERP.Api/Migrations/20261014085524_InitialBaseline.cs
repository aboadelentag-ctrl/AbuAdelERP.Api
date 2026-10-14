using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayanERP.Api.Migrations
{
    /// <summary>
    /// Baseline Migration — يمثل الوضع الحالي للداتابيز.
    /// لا ينفذ أي تغييرات فعلية — فقط يسجّل نقطة البداية
    /// في جدول __EFMigrationsHistory.
    /// </summary>
    public partial class InitialBaseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: كل الجداول موجودة بالفعل في الداتابيز
            // هذه Migration فقط لتسجيل نقطة البداية
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}