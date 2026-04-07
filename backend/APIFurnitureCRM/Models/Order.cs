using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIFurnitureCRM.Models
{
    [Table("Заказ")]
    public class Order
    {
        [Key]
        [Column("Номер_заказа")]
        public int OrderId { get; set; }

        [Column("Артикул_товара")]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Column("Количество")]
        [Required]
        public int Quantity { get; set; } = 1;

        [Column("Дата_оформления")]
        public DateTime OrderDate { get; set; }

        [Column("ID_оформляющего_сотрудника")]
        [ForeignKey("Staff")]
        public int StaffId { get; set; }

        [Column("Дата_выполнения")]
        public DateTime? CompletionDate { get; set; } = null;

        [Column("ID_клиента")]
        [ForeignKey("Client")]
        public int ClientId { get; set; }

        [Column("Итоговая_стоимость")]
        [Required]
        public int TotalPrice { get; set; }

        [Column("Статус_заказа")]
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "В обработке";

        public Product? Product { get; set; }
        public Staff? Staff { get; set; }
        public Client? Client { get; set; }
    }
}
