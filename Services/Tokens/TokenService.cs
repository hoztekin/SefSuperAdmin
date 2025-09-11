using App.Repositories.UserApps;
using App.Services.Authentications.DTOs;
using App.Services.Authentications.Helper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace App.Services.Tokens
{
    public class TokenService(UserManager<UserApp> userManager, IOptions<CustomTokenOptions> tokenOptions) : ITokenService
    {
        public TokenDto CreateToken(UserApp userApp)
        {
            var accessTokenExpiration = DateTime.Now.AddMinutes(tokenOptions.Value.AccessTokenExpiration);
            var refreshTokenExpiration = DateTime.Now.AddMinutes(tokenOptions.Value.RefreshTokenExpiration);
            var securityKey = SignService.GetSymmetricSecurityKey(tokenOptions.Value.SecurityKey);

            SigningCredentials signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(

                issuer: tokenOptions.Value.Issuer,
                expires: accessTokenExpiration,
                 notBefore: DateTime.Now,
                 claims: GetClaims(userApp, tokenOptions.Value.Audience).Result,
                 signingCredentials: signingCredentials);

            var handler = new JwtSecurityTokenHandler();

            var token = handler.WriteToken(jwtSecurityToken);

            var tokenDto = new TokenDto
            {
                AccessToken = token,
                RefreshToken = CreateRefreshToken(),
                AccessTokenExpiration = accessTokenExpiration,
                RefreshTokenExpiration = refreshTokenExpiration
            };

            return tokenDto;
        }

        public ClientTokenDto CreateTokenByClient(Client client)
        {
            var accessTokenExpiration = DateTime.Now.AddMinutes(tokenOptions.Value.AccessTokenExpiration);

            var securityKey = SignService.GetSymmetricSecurityKey(tokenOptions.Value.SecurityKey);

            SigningCredentials signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(

                issuer: tokenOptions.Value.Issuer,
                expires: accessTokenExpiration,
                notBefore: DateTime.Now,
                claims: GetClaimsByClient(client),
                signingCredentials: signingCredentials);

            var handler = new JwtSecurityTokenHandler();

            var token = handler.WriteToken(jwtSecurityToken);

            var tokenDto = new ClientTokenDto
            {
                AccessToken = token,
                AccessTokenExpiration = accessTokenExpiration,
            };

            return tokenDto;
        }

        private async Task<IEnumerable<Claim>> GetClaims(UserApp userApp, string audiences)
        {
            var userRoles = await userManager.GetRolesAsync(userApp);



            var userList = new List<Claim>{
                           new Claim (ClaimTypes.NameIdentifier, userApp.Id),
                           new Claim (JwtRegisteredClaimNames.Email, userApp.Email),
                           new Claim (ClaimTypes.Name, userApp.UserName),
                           new Claim(JwtRegisteredClaimNames.Aud, audiences),
                           new Claim (JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),

            };

            userList.AddRange(userRoles.Select(x => new Claim(ClaimTypes.Role, x)));

            return userList;
        }

        private string CreateRefreshToken()
        {
            var numberByte = new Byte[32];
            using var rnd = RandomNumberGenerator.Create();
            rnd.GetBytes(numberByte);
            return Convert.ToBase64String(numberByte);
        }

        private IEnumerable<Claim> GetClaimsByClient(Client client)
        {
            var claims = new List<Claim>();
            claims.AddRange(client.Audiences.Select(x => new Claim(JwtRegisteredClaimNames.Aud, x)));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, client.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, "client"));

            return claims;
        }
    }
}
