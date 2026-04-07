using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIFurnitureCRM.Models
{
    [Table("Номенклатура")]
    public class Product
    {
        [Key]
        [Column("Артикул_товара")]
        public int ProductId { get; set; }

        [Column("Наименование")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("Категория")]
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Column("Цвет")]
        [Required]
        [MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        [Column("Размеры")]
        [Required]
        [MaxLength(50)]
        public string Dimensions { get; set; } = string.Empty;

        [Column("Стоимость")]
        [Required]
        public int Price { get; set; }

        [Column("Производится")]
        public bool IsProduced { get; set; } = true;

        [JsonIgnore]
        public ICollection<Order>? Orders { get; set; }
        public ICollection<Specification>? Specifications { get; set; }
    }
}