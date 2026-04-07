using System.Text.Json.Serialization;

namespace FurnitureCRMClient.Models
{
    public class SpecificationClient
    {
        [JsonPropertyName("productId")]
        public int Артикул_товара { get; set; }

        [JsonPropertyName("materialId")]
        public int Артикул_сырья { get; set; }

        [JsonPropertyName("quantity")]
        public int Количество { get; set; } = 1;

        public Material? Material { get; set; } // Для отображения названия материала
    }
}
