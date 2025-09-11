using Microsoft.AspNetCore.Identity;

namespace App.Repositories.UserApps
{
    public class UserApp : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
