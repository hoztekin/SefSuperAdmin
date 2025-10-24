namespace App.UI.Application.DTOS
{
    public class CompanyListDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public string TaxNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
