using Microsoft.EntityFrameworkCore;
using APIFurnitureCRM.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace APIFurnitureCRM.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<MaterialOrder> MaterialOrders { get; set; }
        public DbSet<Specification> Specifications { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Связи для Спецификация_номенклатуры
            modelBuilder.Entity<Specification>()
                .HasKey(s => new { s.ProductId, s.MaterialId });

            modelBuilder.Entity<Specification>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Specifications)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Specification>()
                .HasOne(s => s.Material)
                .WithMany(m => m.Specifications)
                .HasForeignKey(s => s.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            // CHECK ограничения для таблицы "Номенклатура"
            modelBuilder.Entity<Product>()
                .ToTable("Номенклатура", tb => tb.HasCheckConstraint("CK_Номенклатура_Категория", "Категория IN ('Мебель для дома', 'Офисная мебель', 'Кухонная мебель', 'Детская мебель', 'Мягкая мебель', 'Корпусная мебель', 'Спальная мебель', 'Мебель для ванной')"));
            modelBuilder.Entity<Product>()
                .ToTable("Номенклатура", tb => tb.HasCheckConstraint("CK_Номенклатура_Стоимость", "Стоимость > 0"));
            // По умолчанию номенклатура считается производимой
            modelBuilder.Entity<Product>()
                .Property(p => p.IsProduced)
                .HasDefaultValue(true);

            // CHECK ограничения для таблицы "Клиент"
            // modelBuilder.Entity<Client>()
            //    .ToTable("Клиент", tb => tb.HasCheckConstraint("CK_Клиент_ДатаРегистрации", "Дата_регистрации <= CURRENT_DATE"));

            // CHECK ограничения для таблицы "Заказ"
            modelBuilder.Entity<Order>()
                .ToTable("Заказ", tb => tb.HasCheckConstraint("CK_Заказ_Количество", "Количество >= 1"));
            modelBuilder.Entity<Order>()
                .ToTable("Заказ", tb => tb.HasCheckConstraint("CK_Заказ_ИтоговаяСтоимость", "Итоговая_стоимость > 0"));
            modelBuilder.Entity<Order>()
                .ToTable("Заказ", tb => tb.HasCheckConstraint("CK_Заказ_СтатусЗаказа", "Статус_заказа IN ('В обработке', 'В производстве', 'Выполнен', 'Отменен')"));
            modelBuilder.Entity<Order>()
                .ToTable("Заказ", tb => tb.HasCheckConstraint("CK_Заказ_ДатаОформления", "DATE(Дата_оформления) <= CURRENT_DATE"));
            modelBuilder.Entity<Order>()
                .ToTable("Заказ", tb => tb.HasCheckConstraint("CK_Заказ_ДатыВыполнения", "Дата_выполнения IS NULL OR DATE(Дата_выполнения) >= DATE(Дата_оформления)"));
            // Дата оформления заказа генерируется БД (DEFAULT DATETIME('now','localtime')),
            // поэтому помечаем свойство как вычисляемое при вставке.
            modelBuilder.Entity<Order>()
                .Property(o => o.OrderDate)
                .HasDefaultValueSql("DATE('now')")
                .ValueGeneratedOnAdd();

            // CHECK ограничения для таблицы "Сырьё"
            modelBuilder.Entity<Material>()
                .ToTable("Сырьё", tb => tb.HasCheckConstraint("CK_Сырьё_КоличествоВНаличии", "Количество_в_наличии >= 0"));

            // CHECK ограничения для таблицы "Заказ_сырья"
            modelBuilder.Entity<MaterialOrder>()
                .ToTable("Заказ_сырья", tb => tb.HasCheckConstraint("CK_ЗаказСырья_Количество", "Количество >= 1"));
            modelBuilder.Entity<MaterialOrder>()
                .Property(m => m.OrderDate)
                .HasDefaultValueSql("DATE('now')")
                .ValueGeneratedOnAdd();
            // CHECK по дате заказа сырья: дата оформления не позже сегодняшней (используем CURRENT_DATE, а не DATE('now'))
            modelBuilder.Entity<MaterialOrder>()
                .ToTable("Заказ_сырья", tb => tb.HasCheckConstraint("CK_ЗаказСырья_ДатаЗаказа", "Дата_заказа <= CURRENT_DATE"));
            modelBuilder.Entity<MaterialOrder>()
                .ToTable("Заказ_сырья", tb => tb.HasCheckConstraint("CK_ЗаказСырья_Статус", "Статус IN ('Ожидает поставки', 'Доставлен', 'Отменен')"));

            // CHECK ограничения для таблицы "Спецификация_номенклатуры"
            modelBuilder.Entity<Specification>()
                .ToTable("Спецификация_номенклатуры", tb => tb.HasCheckConstraint("CK_СпецификацияНоменклатуры_Количество", "Количество >= 1"));

            // Уникальные ограничения для таблицы "Учетные_записи_пользователей"
            modelBuilder.Entity<UserAccount>()
                .HasIndex(ua => ua.Username)
                .IsUnique();
            modelBuilder.Entity<UserAccount>()
                .HasIndex(ua => ua.StaffId)
                .IsUnique();
            // По умолчанию учетная запись активна (сотрудник в штате)
            modelBuilder.Entity<UserAccount>()
                .Property(ua => ua.IsActive)
                .HasDefaultValue(true);

        }
    }
}
