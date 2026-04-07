using System;
using System.Text.Json.Serialization;

namespace FurnitureCRMClient.Models
{
    public class MaterialOrder
    {
        [JsonPropertyName("materialOrderId")]
        public int Номер_заказа { get; set; }

        [JsonPropertyName("supplier")]
        public string Поставщик { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int Артикул_сырья { get; set; }

        [JsonPropertyName("quantity")]
        public int Количество { get; set; } = 1;

        // Дату заказа получаем и отображаем из API,
        // но при отправке (POST/PUT) клиент её не шлёт — ей управляет сервер/БД.
        [JsonPropertyName("orderDate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DateTime Дата_заказа { get; set; }

        [JsonPropertyName("staffId")]
        public int ID_оформляющего_сотрудника { get; set; }

        [JsonPropertyName("status")]
        public string Статус { get; set; } = "Ожидает поставки";

        [JsonIgnore]
        public string Наименование_сырья { get; set; }

        [JsonIgnore]
        public string ФИО_оформителя { get; set; }
    }
}
