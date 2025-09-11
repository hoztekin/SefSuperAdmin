using App.Services.Authentications.DTOs;
using System.Text.Json.Serialization;

namespace App.UI.DTOS
{
    public class AuthResponseDto
    {
        public AuthDataDto Data { get; set; }
        public string ResponseType { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
    public class AuthDataDto
    {
        public TokenDto Token { get; set; }

        [JsonPropertyName("user-info")]
        public UserInfoDto UserInfo { get; set; }

        [JsonPropertyName("authorized-staffs")]

        public List<string> Roles { get; set; }
    }
}
