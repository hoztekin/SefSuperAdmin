using Microsoft.AspNetCore.Identity;

namespace App.Repositories.UserRoles
{
    public class UserRole : IdentityRole
    {
        public string? Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }

}
