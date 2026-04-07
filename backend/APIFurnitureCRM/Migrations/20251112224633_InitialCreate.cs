using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIFurnitureCRM.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Клиент",
                columns: table => new
                {
                    ID_клиента = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ФИО = table.Column<string>(type: "TEXT", nullable: false),
                    Номер_телефона = table.Column<long>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Адрес = table.Column<string>(type: "TEXT", nullable: true),
                    Дата_регистрации = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Клиент", x => x.ID_клиента);
                });

            migrationBuilder.CreateTable(
                name: "Номенклатура",
                columns: table => new
                {
                    Артикул_товара = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Наименование = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Категория = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Цвет = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Размеры = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Стоимость = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Номенклатура", x => x.Артикул_товара);
                    table.CheckConstraint("CK_Номенклатура_Категория", "Категория IN ('Мебель для дома', 'Офисная мебель', 'Кухонная мебель', 'Детская мебель', 'Мягкая мебель', 'Корпусная мебель', 'Спальная мебель', 'Мебель для ванной')");
                    table.CheckConstraint("CK_Номенклатура_Стоимость", "Стоимость > 0");
                });

            migrationBuilder.CreateTable(
                name: "Персонал",
                columns: table => new
                {
                    ID_сотрудника = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ФИО = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Должность = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Персонал", x => x.ID_сотрудника);
                });

            migrationBuilder.CreateTable(
                name: "Сырьё",
                columns: table => new
                {
                    Артикул_сырья = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Наименование_материала = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Единица_измерения = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Количество_в_наличии = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Сырьё", x => x.Артикул_сырья);
                    table.CheckConstraint("CK_Сырьё_КоличествоВНаличии", "Количество_в_наличии >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Заказ",
                columns: table => new
                {
                    Номер_заказа = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Артикул_товара = table.Column<int>(type: "INTEGER", nullable: false),
                    Количество = table.Column<int>(type: "INTEGER", nullable: false),
                    Дата_оформления = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ID_оформляющего_сотрудника = table.Column<int>(type: "INTEGER", nullable: false),
                    Дата_выполнения = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ID_клиента = table.Column<int>(type: "INTEGER", nullable: false),
                    Итоговая_стоимость = table.Column<int>(type: "INTEGER", nullable: false),
                    Статус_заказа = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Заказ", x => x.Номер_заказа);
                    table.CheckConstraint("CK_Заказ_ДатаОформления", "Дата_оформления <= CURRENT_DATE");
                    table.CheckConstraint("CK_Заказ_ДатыВыполнения", "Дата_выполнения IS NULL OR Дата_выполнения >= Дата_оформления");
                    table.CheckConstraint("CK_Заказ_ИтоговаяСтоимость", "Итоговая_стоимость > 0");
                    table.CheckConstraint("CK_Заказ_Количество", "Количество >= 1");
                    table.CheckConstraint("CK_Заказ_СтатусЗаказа", "Статус_заказа IN ('В обработке', 'В производстве', 'Выполнен', 'Отменен')");
                    table.ForeignKey(
                        name: "FK_Заказ_Клиент_ID_клиента",
                        column: x => x.ID_клиента,
                        principalTable: "Клиент",
                        principalColumn: "ID_клиента",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Заказ_Номенклатура_Артикул_товара",
                        column: x => x.Артикул_товара,
                        principalTable: "Номенклатура",
                        principalColumn: "Артикул_товара",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Заказ_Персонал_ID_оформляющего_сотрудника",
                        column: x => x.ID_оформляющего_сотрудника,
                        principalTable: "Персонал",
                        principalColumn: "ID_сотрудника",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Учетные_записи_пользователей",
                columns: table => new
                {
                    ID_учетной_записи = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Логин = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Пароль = table.Column<string>(type: "TEXT", nullable: false),
                    ID_сотрудника = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Учетные_записи_пользователей", x => x.ID_учетной_записи);
                    table.ForeignKey(
                        name: "FK_Учетные_записи_пользователей_Персонал_ID_сотрудника",
                        column: x => x.ID_сотрудника,
                        principalTable: "Персонал",
                        principalColumn: "ID_сотрудника",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Заказ_сырья",
                columns: table => new
                {
                    Номер_заказа = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Поставщик = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Артикул_сырья = table.Column<int>(type: "INTEGER", nullable: false),
                    Количество = table.Column<int>(type: "INTEGER", nullable: false),
                    Дата_заказа = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ID_оформляющего_сотрудника = table.Column<int>(type: "INTEGER", nullable: false),
                    Статус = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Заказ_сырья", x => x.Номер_заказа);
                    table.CheckConstraint("CK_ЗаказСырья_ДатаЗаказа", "Дата_заказа <= CURRENT_DATE");
                    table.CheckConstraint("CK_ЗаказСырья_Количество", "Количество >= 1");
                    table.CheckConstraint("CK_ЗаказСырья_Статус", "Статус IN ('Ожидает поставки', 'Доставлен', 'Отменен')");
                    table.ForeignKey(
                        name: "FK_Заказ_сырья_Персонал_ID_оформляющего_сотрудника",
                        column: x => x.ID_оформляющего_сотрудника,
                        principalTable: "Персонал",
                        principalColumn: "ID_сотрудника",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Заказ_сырья_Сырьё_Артикул_сырья",
                        column: x => x.Артикул_сырья,
                        principalTable: "Сырьё",
                        principalColumn: "Артикул_сырья",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Спецификация_номенклатуры",
                columns: table => new
                {
                    Артикул_товара = table.Column<int>(type: "INTEGER", nullable: false),
                    Артикул_сырья = table.Column<int>(type: "INTEGER", nullable: false),
                    Количество = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Спецификация_номенклатуры", x => new { x.Артикул_товара, x.Артикул_сырья });
                    table.CheckConstraint("CK_СпецификацияНоменклатуры_Количество", "Количество >= 1");
                    table.ForeignKey(
                        name: "FK_Спецификация_номенклатуры_Номенклатура_Артикул_товара",
                        column: x => x.Артикул_товара,
                        principalTable: "Номенклатура",
                        principalColumn: "Артикул_товара",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Спецификация_номенклатуры_Сырьё_Артикул_сырья",
                        column: x => x.Артикул_сырья,
                        principalTable: "Сырьё",
                        principalColumn: "Артикул_сырья",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Заказ_Артикул_товара",
                table: "Заказ",
                column: "Артикул_товара");

            migrationBuilder.CreateIndex(
                name: "IX_Заказ_ID_клиента",
                table: "Заказ",
                column: "ID_клиента");

            migrationBuilder.CreateIndex(
                name: "IX_Заказ_ID_оформляющего_сотрудника",
                table: "Заказ",
                column: "ID_оформляющего_сотрудника");

            migrationBuilder.CreateIndex(
                name: "IX_Заказ_сырья_Артикул_сырья",
                table: "Заказ_сырья",
                column: "Артикул_сырья");

            migrationBuilder.CreateIndex(
                name: "IX_Заказ_сырья_ID_оформляющего_сотрудника",
                table: "Заказ_сырья",
                column: "ID_оформляющего_сотрудника");

            migrationBuilder.CreateIndex(
                name: "IX_Спецификация_номенклатуры_Артикул_сырья",
                table: "Спецификация_номенклатуры",
                column: "Артикул_сырья");

            migrationBuilder.CreateIndex(
                name: "IX_Учетные_записи_пользователей_Логин",
                table: "Учетные_записи_пользователей",
                column: "Логин",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Учетные_записи_пользователей_ID_сотрудника",
                table: "Учетные_записи_пользователей",
                column: "ID_сотрудника",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Заказ");

            migrationBuilder.DropTable(
                name: "Заказ_сырья");

            migrationBuilder.DropTable(
                name: "Спецификация_номенклатуры");

            migrationBuilder.DropTable(
                name: "Учетные_записи_пользователей");

            migrationBuilder.DropTable(
                name: "Клиент");

            migrationBuilder.DropTable(
                name: "Номенклатура");

            migrationBuilder.DropTable(
                name: "Сырьё");

            migrationBuilder.DropTable(
                name: "Персонал");
        }
    }
}
