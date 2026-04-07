using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIFurnitureCRM.Models
{
    [Table("Заказ_сырья")]
    public class MaterialOrder
    {
        [Key]
        [Column("Номер_заказа")]
        public int MaterialOrderId { get; set; }

        [Column("Поставщик")]
        [Required]
        [MaxLength(100)]
        public string Supplier { get; set; } = string.Empty;

        [Column("Артикул_сырья")]
        [ForeignKey("Material")]
        public int MaterialId { get; set; }

        [Column("Количество")]
        [Required]
        public int Quantity { get; set; } = 1;

        [Column("Дата_заказа")]
        public DateTime? OrderDate { get; set; }

        [Column("ID_оформляющего_сотрудника")]
        [ForeignKey("Staff")]
        public int StaffId { get; set; }

        [Column("Статус")]
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Ожидает поставки";

        public Material? Material { get; set; }
        public Staff? Staff { get; set; }
    }
}
