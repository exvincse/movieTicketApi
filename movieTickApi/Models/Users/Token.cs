namespace movieTickApi.Models.Users
{
        public class Token
        {
                public Guid Id { get; set; }
                public Guid UserId { get; set; }
                public required string AccessToken { get; set; }
                public DateTime CreateDateTime { get; set; }
                public DateTime ExpiryDateTime { get; set; }
                public bool IsRevoked { get; set; }
                public UserRefreshTokens UserRefreshTokens { get; set; }
        }
}
