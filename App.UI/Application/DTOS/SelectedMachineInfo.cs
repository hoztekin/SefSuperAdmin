namespace App.UI.Application.DTOS
{
    public class SelectedMachineInfo
    {
        public int MachineId { get; set; }
        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string ApiAddress { get; set; } = string.Empty;
        public DateTime SelectedAt { get; set; }
        public DateTime? LastHealthCheck { get; set; }
    }
}
