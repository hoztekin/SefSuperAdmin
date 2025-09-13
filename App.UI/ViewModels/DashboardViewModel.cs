namespace App.UI.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }
        public int ActiveBranches { get; set; }
        public int IpOperations { get; set; }
        public int PendingApprovals { get; set; }
        public int OnlineUsers { get; set; }
        public int DailyOperations { get; set; }
        public int SecurityScore { get; set; }
    }
}
