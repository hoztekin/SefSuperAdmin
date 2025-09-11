using App.Services.Authentications;
using App.Services.Authentications.DTOs;
using App.Services.Authentications.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{

    public class AuthController(IAuthenticationService authenticationService) : CustomBaseController
    {
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> CreateToken(LoginDto loginDto) => CreateActionResult(await authenticationService.CreateTokenAsync(loginDto));

        [AllowAnonymous]
        [HttpPost("CreateTokenByClient")]
        public IActionResult CreateTokenByClient(ClientLoginDto clientLoginDto) => CreateActionResult(authenticationService.CreateTokenByClient(clientLoginDto));


        [HttpPost("RevokeRefreshToken")]
        public async Task<IActionResult> RevokeRefreshToken(RefreshTokenDto refreshTokenDto) => CreateActionResult(await authenticationService.RevokeRefreshToken(refreshTokenDto.Token));

        [AllowAnonymous]
        [HttpPost("CreateTokenByRefreshToken")]
        public async Task<IActionResult> CreateTokenByRefreshToken(RefreshTokenDto refreshTokenDto) =>
                                                                                  CreateActionResult(await authenticationService.CreateTokenByRefreshTokenAsync(refreshTokenDto.Token));

    }
}
