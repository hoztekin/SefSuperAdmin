using App.UI.Application.DTOS;

namespace App.UI.Presentation.ViewModels
{
    public class DashboardViewModel
    {
        public SelectedMachineInfo SelectedMachine { get; set; }
        public bool HasSelectedMachine { get; set; }
        public DashboardStats Stats { get; set; }
        public ApiHealthInfo ApiHealth { get; set; }
        public List<RecentActivity> RecentActivities { get; set; }
        public bool ShowMachineModal { get; set; }

        public DashboardViewModel()
        {
            Stats = new DashboardStats();
            ApiHealth = new ApiHealthInfo();
            RecentActivities = new List<RecentActivity>();
        }
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; } = 0;
        public string LicenseExpiry { get; set; } = "Bilinmiyor";
        public int BranchCount { get; set; } = 0;
        public int ActiveLicenses { get; set; } = 0;
    }

    public class ApiHealthInfo
    {
        public bool IsHealthy { get; set; } = false;
        public string ResponseTime { get; set; } = "---";
        public string Uptime { get; set; } = "---";
        public int ActiveServices { get; set; } = 0;
        public string StatusMessage { get; set; } = "API Bağlantısı Kontrol Ediliyor";
    }

    public class RecentActivity
    {
        public string Icon { get; set; }
        public string IconColor { get; set; }
        public string Message { get; set; }
        public string TimeAgo { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
