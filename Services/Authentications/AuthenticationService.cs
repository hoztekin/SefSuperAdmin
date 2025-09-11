using App.Repositories;
using App.Repositories.UserApps;
using App.Repositories.UserRefreshTokens;
using App.Services.Authentications.DTOs;
using App.Services.Authentications.Helper;
using App.Services.Authentications.Login;
using App.Services.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;

namespace App.Services.Authentications
{
    public class AuthenticationService(IOptions<List<Client>> optionsClient, ITokenService tokenService, UserManager<UserApp> userManager, IUnitOfWork unitOfWork, IUserRefreshTokenRepository userRefreshTokenRepository) : IAuthenticationService
    {
        public async Task<ServiceResult<TokenDto>> CreateTokenAsync(LoginDto loginDto)
        {
            if (loginDto == null) throw new ArgumentException(nameof(loginDto));

            var user = await userManager.FindByNameAsync(loginDto.UserName);

            if (user == null) return ServiceResult<TokenDto>.Fail("EMail or Password is wrong", HttpStatusCode.NotFound);

            if (!await userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return ServiceResult<TokenDto>.Fail("EMail or Password is wrong", HttpStatusCode.Found);
            }
            var token = tokenService.CreateToken(user);

            var userRefreshToken = await userRefreshTokenRepository.Where(x => x.UserId == user.Id).SingleOrDefaultAsync();

            if (userRefreshToken == null)
            {
                await userRefreshTokenRepository.AddAsync(new UserRefreshToken
                {
                    UserId = user.Id,
                    Code = token.RefreshToken,
                    Expiration = token.RefreshTokenExpiration
                });
            }

            else
            {
                userRefreshToken.Code = token.RefreshToken;
                userRefreshToken.Expiration = token.RefreshTokenExpiration;
                userRefreshTokenRepository.Update(userRefreshToken);
            }

            await unitOfWork.SaveChangesAsync();

            return ServiceResult<TokenDto>.Success(token);
        }

        public ServiceResult<ClientTokenDto> CreateTokenByClient(ClientLoginDto clientLoginDto)
        {
            var client = optionsClient.Value.SingleOrDefault(x => x.Id == clientLoginDto.ClientId && x.Secret == clientLoginDto.ClientSecret);

            if (client == null)
            {
                return ServiceResult<ClientTokenDto>.Fail("Client or ClientSecret not found", HttpStatusCode.NotFound);
            }

            var token = tokenService.CreateTokenByClient(client);

            return ServiceResult<ClientTokenDto>.Success(token);
        }

        public async Task<ServiceResult<TokenDto>> CreateTokenByRefreshTokenAsync(string refreshToken)
        {
            var existRefreshToken = await userRefreshTokenRepository.Where(x => x.Code == refreshToken).SingleOrDefaultAsync();

            if (existRefreshToken == null)
            {
                return ServiceResult<TokenDto>.Fail("Refresh token not found", HttpStatusCode.NotFound);
            }

            var user = await userManager.FindByIdAsync(existRefreshToken.UserId);

            if (user == null)
            {

                return ServiceResult<TokenDto>.Fail("User Id not found", HttpStatusCode.NotFound);
            }

            var tokenDto = tokenService.CreateToken(user);

            existRefreshToken.Code = tokenDto.RefreshToken;
            existRefreshToken.Expiration = tokenDto.RefreshTokenExpiration;


            await unitOfWork.SaveChangesAsync();

            return ServiceResult<TokenDto>.Success(tokenDto, HttpStatusCode.OK);
        }

        public async Task<ServiceResult> RevokeRefreshToken(string refreshToken)
        {
            var existResfreshToken = await userRefreshTokenRepository.Where(x => x.Code == refreshToken).SingleOrDefaultAsync();

            if (existResfreshToken == null)
            {
                return ServiceResult.Fail("Refresh token not found", HttpStatusCode.NotFound);
            }

            userRefreshTokenRepository.Delete(existResfreshToken);

            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
    }
}
