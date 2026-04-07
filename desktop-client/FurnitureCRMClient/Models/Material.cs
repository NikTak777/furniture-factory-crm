using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FurnitureCRMClient.Models
{
    public class Material
    {
        [JsonPropertyName("materialId")]
        public int Артикул_сырья { get; set; }
        [JsonPropertyName("materialName")]
        public string Наименование_материала { get; set; } = string.Empty;
        [JsonPropertyName("unit")]
        public string Единица_измерения { get; set; } = string.Empty;
        [JsonPropertyName("quantityAvailable")]
        public int Количество_в_наличии { get; set; }
    }
}
