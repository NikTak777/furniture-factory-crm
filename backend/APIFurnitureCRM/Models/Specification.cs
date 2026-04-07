using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIFurnitureCRM.Models
{
    [Table("Спецификация_номенклатуры")]
    public class Specification
    {
        [Column("Артикул_товара")]
        public int ProductId { get; set; }

        [Column("Артикул_сырья")]
        [JsonPropertyName("MaterialId")]
        public int MaterialId { get; set; }

        [Column("Количество")]
        public int Quantity { get; set; } = 1;

        // Навигационные свойства (для связей)
        [JsonIgnore]
        public Product? Product { get; set; }
        [JsonIgnore]
        public Material? Material { get; set; }
    }
}