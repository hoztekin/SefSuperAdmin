namespace App.UI.Application.DTOS
{
    public class ExternalUserListDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string BranchName { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
