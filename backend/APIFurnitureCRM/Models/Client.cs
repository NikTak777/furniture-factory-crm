using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APIFurnitureCRM.Models
{
    [Table("Клиент")]
    public class Client
    {
        [Key]
        [Column("ID_клиента")]
        public int Id { get; set; }

        [Column("ФИО")]
        [Required]
        public string FullName { get; set; } = "";

        [Column("Номер_телефона")]
        [Required]
        public long Phone { get; set; }

        [Column("Email")]
        public string? Email { get; set; }

        [Column("Адрес")]
        public string? Address { get; set; }

        [Column("Дата_регистрации")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
