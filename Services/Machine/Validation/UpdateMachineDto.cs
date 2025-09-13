namespace App.Services.Machine.Validation
{
    public class UpdateMachineDto
    {
        public int Id { get; set; }
        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string ApiAddress { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
