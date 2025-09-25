namespace App.UI.Application.DTOS
{
    public class ExternalUserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int RecentUsers { get; set; } // Son 30 gün
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}
