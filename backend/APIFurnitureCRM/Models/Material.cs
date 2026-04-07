using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIFurnitureCRM.Models
{
    [Table("Сырьё")]
    public class Material
    {
        [Key]
        [Column("Артикул_сырья")]
        public int MaterialId { get; set; }

        [Column("Наименование_материала")]
        [Required]
        [MaxLength(100)]
        public string MaterialName { get; set; } = string.Empty;

        [Column("Единица_измерения")]
        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Column("Количество_в_наличии")]
        public int QuantityAvailable { get; set; } = 0;

        public ICollection<MaterialOrder>? MaterialOrders { get; set; }
        
        [JsonIgnore]
        public ICollection<Specification>? Specifications { get; set; }
    }
}
