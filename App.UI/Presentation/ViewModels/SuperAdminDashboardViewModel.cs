using App.UI.Application.DTOS;

namespace App.UI.Presentation.ViewModels
{
    public class SuperAdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMachines { get; set; }
        public int TotalRoles { get; set; }
        public int ActiveMachines { get; set; }
        public List<UserAppDtoUI> RecentUsers { get; set; } = new();
        public List<MachineListViewModel> RecentMachines { get; set; } = new();
    }
}
