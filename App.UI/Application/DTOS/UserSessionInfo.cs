namespace App.UI.Application.DTOS
{
    public class UserSessionInfo
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto UserInfo { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
