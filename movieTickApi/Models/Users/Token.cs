namespace movieTickApi.Models.Users
{
        public class Token
        {
                public Guid Id { get; set; }
                public Guid UserId { get; set; }
                public required string token { get; set; }
                public string? TokenHash { get; set; }
                public DateTime CreatedAt { get; set; }
                public DateTime ExpiresAt { get; set; }
                public bool IsRevoked { get; set; }
                public UserRefreshTokens UserRefreshTokens { get; set; }
        }
}
