namespace App.Repositories.UserRefreshTokens
{
    public class UserRefreshToken : BaseEntity
    {      
        public required string UserId { get; set; }
        public required string Code { get; set; }
        public DateTime Expiration { get; set; }
    }
}
