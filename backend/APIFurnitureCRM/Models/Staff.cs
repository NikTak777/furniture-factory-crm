using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIFurnitureCRM.Models
{
    [Table("Персонал")]
    public class Staff
    {
        [Key]
        [Column("ID_сотрудника")]
        public int StaffId { get; set; }

        [Column("ФИО")]
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Column("Должность")]
        [Required]
        [MaxLength(50)]
        public string Position { get; set; } = string.Empty;

        public ICollection<Order>? Orders { get; set; }
        public ICollection<MaterialOrder>? MaterialOrders { get; set; }

        public UserAccount? UserAccount { get; set; }
    }
}
