namespace App.Services.Machine.Dtos
{
    public class CreateMachineDto
    {
        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string ApiAddress { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
