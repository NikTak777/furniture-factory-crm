using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIFurnitureCRM.Migrations
{
    public partial class FixDateCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Удаляем старое ограничение по дате оформления заказа сырья.
            // Из-за ограничений новых версий SQLite (non-deterministic use of date())
            // CHECK с использованием DATE('now') больше не создаём, используем CURRENT_DATE.
            migrationBuilder.DropCheckConstraint(
                name: "CK_ЗаказСырья_ДатаЗаказа",
                table: "Заказ_сырья");

            // Создаём новое ограничение: дата заказа сырья не позже сегодняшней даты (CURRENT_DATE)
            migrationBuilder.AddCheckConstraint(
                name: "CK_ЗаказСырья_ДатаЗаказа",
                table: "Заказ_сырья",
                sql: "Дата_заказа <= CURRENT_DATE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат — вернуть старую версию (если понадобится)
            migrationBuilder.DropCheckConstraint(
                name: "CK_ЗаказСырья_ДатаЗаказа",
                table: "Заказ_сырья");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ЗаказСырья_ДатаЗаказа",
                table: "Заказ_сырья",
                sql: "Дата_заказа <= CURRENT_DATE"); // как было раньше
        }
    }
}