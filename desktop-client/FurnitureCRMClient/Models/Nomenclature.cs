using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace FurnitureCRMClient.Models
{
    public class Nomenclature
    {
        [JsonPropertyName("productId")]
        public int Артикул_товара { get; set; }

        [JsonPropertyName("name")]
        public string Наименование { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Категория { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Цвет { get; set; } = string.Empty;

        [JsonPropertyName("dimensions")]
        public string Размеры { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public int Стоимость { get; set; }

        // Статус: производится ли товар сейчас (true – в производстве, false – снят)
        [JsonPropertyName("isProduced")]
        public bool Производится { get; set; } = true;

        [JsonPropertyName("specifications")]
        public ICollection<SpecificationClient>? Specifications { get; set; }
    }
}
