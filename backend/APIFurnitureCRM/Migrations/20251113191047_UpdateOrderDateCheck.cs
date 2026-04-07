using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIFurnitureCRM.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderDateCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Заказ_ДатаОформления",
                table: "Заказ");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Заказ_ДатыВыполнения",
                table: "Заказ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Заказ_ДатаОформления",
                table: "Заказ",
                sql: "DATE(Дата_оформления) <= CURRENT_DATE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Заказ_ДатыВыполнения",
                table: "Заказ",
                sql: "Дата_выполнения IS NULL OR DATE(Дата_выполнения) >= DATE(Дата_оформления)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Заказ_ДатаОформления",
                table: "Заказ");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Заказ_ДатыВыполнения",
                table: "Заказ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Заказ_ДатаОформления",
                table: "Заказ",
                sql: "Дата_оформления <= CURRENT_DATE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Заказ_ДатыВыполнения",
                table: "Заказ",
                sql: "Дата_выполнения IS NULL OR Дата_выполнения >= Дата_оформления");
        }
    }
}
