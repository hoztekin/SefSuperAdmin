using App.Shared.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace App.Repositories.UserApps
{
    public class UserApp : IdentityUser, IAuditable
    {
        public UserApp()
        {
            CreatedDate = DateTime.UtcNow;
            CreatedBy = string.Empty;
            UpdatedBy = string.Empty;
            IsDeleted = false;
            IsActive = true;
        }

        public string? FullName { get; set; }

        [JsonConverter(typeof(UtcDateTimeJsonConverter))]
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        [JsonConverter(typeof(NullableUtcDateTimeJsonConverter))]
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
