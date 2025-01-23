namespace movieTickApi.Models
{
        public class Token
        {
                public Guid Id { get; set; }
                public Guid UserId { get; set; }
                public string token { get; set; }
                public string? TokenHash { get; set; }
                public DateTime CreatedAt { get; set; }
                public DateTime ExpiresAt { get; set; }
                public Boolean IsRevoked { get; set; }
                public UserRefreshTokens UserRefreshTokens { get; set; }
        }
}
