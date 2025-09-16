using Microsoft.AspNetCore.Identity;

namespace App.Repositories.UserRoles
{
    public class UserRole : IdentityRole
    {
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

}
