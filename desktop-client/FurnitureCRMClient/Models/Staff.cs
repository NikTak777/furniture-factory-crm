using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FurnitureCRMClient.Models; // Добавляем using для UserAccount

namespace FurnitureCRMClient.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        public UserAccount? UserAccount { get; set; } // Добавляем навигационное свойство для UserAccount

        public override bool Equals(object? obj)
        {
            return obj is Staff staff &&
                   StaffId == staff.StaffId &&
                   FullName == staff.FullName &&
                   Position == staff.Position &&
                   EqualityComparer<UserAccount?>.Default.Equals(UserAccount, staff.UserAccount);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StaffId, FullName, Position, UserAccount);
        }
    }
}
