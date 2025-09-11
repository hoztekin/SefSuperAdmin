namespace App.Services.Authentications.DTOs
{
    public class ClientTokenDto
    {
        public string AccessToken { get; set; }

        public DateTime AccessTokenExpiration { get; set; }
    }
}
