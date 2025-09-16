using Microsoft.AspNetCore.Identity;

namespace App.Repositories.UserApps
{
    public class UserApp : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
