using App.Repositories.UserApps;
using App.Services.Authentications.DTOs;
using App.Services.Authentications.Helper;

namespace App.Services.Tokens
{
    public interface ITokenService
    {
        TokenDto CreateToken(UserApp userApp);

        ClientTokenDto CreateTokenByClient(Client client);
    }
}
