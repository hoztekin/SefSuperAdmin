namespace App.UI.Application.DTOS
{
    public class BranchDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ContactDetails { get; set; }
        public int NumberOfEmployees { get; set; }
        public string? OperatingHours { get; set; }
        public bool IsActive { get; set; }
    }
}
