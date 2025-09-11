using App.Services.Authentications.DTOs;
using App.Services.Authentications.Login;

namespace App.Services.Authentications
{
    public interface IAuthenticationService
    {
        Task<ServiceResult<TokenDto>> CreateTokenAsync(LoginDto loginDto);

        Task<ServiceResult<TokenDto>> CreateTokenByRefreshTokenAsync(string refreshToken);

        Task<ServiceResult> RevokeRefreshToken(string refreshToken);

        ServiceResult<ClientTokenDto> CreateTokenByClient(ClientLoginDto clientLoginDto);
    }
}
