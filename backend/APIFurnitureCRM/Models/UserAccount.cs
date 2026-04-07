using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; 

namespace APIFurnitureCRM.Models
{
    [Table("Учетные_записи_пользователей")]
    public class UserAccount
    {
        [Key]
        [Column("ID_учетной_записи")]
        public int UserAccountId { get; set; }

        [Column("Логин")]
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Column("Пароль")]
        [Required]
        public string Password { get; set; } = string.Empty;

        [Column("ID_сотрудника")]
        [ForeignKey("Staff")]
        public int StaffId { get; set; }

        [Column("Активен")]
        public bool IsActive { get; set; } = true;

        [JsonIgnore] 
        public Staff? Staff { get; set; } 
    }
}
