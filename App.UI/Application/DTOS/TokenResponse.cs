namespace App.UI.Application.DTOS
{
    public class TokenResponse
    {
        public TokenDtoUI Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> ErrorMessage { get; set; }
    }
}
