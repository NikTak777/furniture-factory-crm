using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FurnitureCRMClient.Models
{
    public class Client
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fullName")]
        [Required]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public long Phone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("registrationDate")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
