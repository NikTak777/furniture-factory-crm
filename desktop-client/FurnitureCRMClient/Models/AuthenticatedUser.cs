using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace FurnitureCRMClient.Models
{
    public class AuthenticatedUser
    {
        [JsonPropertyName("id_сотрудника")] 
        public int ID_сотрудника { get; set; }
        [JsonPropertyName("фИО")] 
        public string ФИО { get; set; } = string.Empty;
        [JsonPropertyName("должность")] 
        public string Должность { get; set; } = string.Empty;
        // Добавьте любые другие свойства, которые ваш API будет возвращать
        // например, токен аутентификации
        // public string Token { get; set; }
    }
}