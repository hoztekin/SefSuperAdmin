namespace App.Repositories.UserRefreshTokens
{
    public class UserRefreshToken : BaseEntity<string>
    {
        public UserRefreshToken()
        {
            Id = Guid.NewGuid().ToString();
        }
        public required string UserId { get; set; }
        public required string Code { get; set; }
        public DateTime Expiration { get; set; }
    }
}
