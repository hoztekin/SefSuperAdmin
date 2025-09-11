namespace App.UI.DTOS
{
    public class TokenResponse
    {
        public TokenData Data { get; set; }
        public bool IsSuccessful { get; set; }
        public string Error { get; set; }
    }

    public class TokenData
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiration { get; set; }
        public string RefreshToken { get; set; }
    }
}
