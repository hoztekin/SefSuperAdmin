namespace App.UI.Application.DTOS
{
    public class ExternalApiLoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int ExpiresIn { get; set; } // seconds
    }
}
