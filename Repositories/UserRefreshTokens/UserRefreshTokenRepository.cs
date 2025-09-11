namespace App.Repositories.UserRefreshTokens
{
    public class UserRefreshTokenRepository(AppDbContext context) : GenericRepository<UserRefreshToken, string>(context), IUserRefreshTokenRepository
    {

    }
}
