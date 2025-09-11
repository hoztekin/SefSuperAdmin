using App.Shared.Helpers;
using System.Text.Json.Serialization;

namespace App.Repositories
{
    public class BaseEntity
    {
        public BaseEntity()
        {
            Code = string.Empty;
            IsDeleted = false;
            IsActive = true;
            CreatedDate = DateTime.UtcNow;
            CreatedBy = string.Empty;
            UpdatedBy = string.Empty;
        }

        public int Id { get; set; } = default!;

        public string Code { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public bool IsActive { get; set; } = true;

        [JsonConverter(typeof(UtcDateTimeJsonConverter))]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        [JsonConverter(typeof(NullableUtcDateTimeJsonConverter))]
        public DateTime? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }
}
