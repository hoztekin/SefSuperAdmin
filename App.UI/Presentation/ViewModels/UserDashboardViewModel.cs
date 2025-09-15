using App.UI.Application.DTOS;

namespace App.UI.Presentation.ViewModels
{
    public class UserDashboardViewModel
    {
        public string UserName { get; set; }
        public SelectedMachineInfo SelectedMachine { get; set; }
        public bool HasSelectedMachine { get; set; }
        public DateTime? LastHealthCheck { get; set; }
    }
}
