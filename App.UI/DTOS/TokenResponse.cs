namespace App.UI.DTOS
{
    public class TokenResponse
    {
        public TokenDtoUI Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> ErrorMessage { get; set; }
    }

    public class TokenData
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiration { get; set; }
        public string RefreshToken { get; set; }
    }
}
