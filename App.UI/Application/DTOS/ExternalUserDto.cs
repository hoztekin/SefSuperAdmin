namespace App.UI.Application.DTOS
{
    public class ExternalUserDto
    {
        public string Id { get; set; }
        public string Username { get; set; } 
        public string EMail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BranchId { get; set; }
        public string BranchName { get; set; }
        public bool IsActive { get; set; }

        // View için ek özellikler
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime? CreatedDate { get; set; } 
        public bool EmailConfirmed { get; set; } = true;

    }
}
