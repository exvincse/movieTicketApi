namespace movieTickApi.Models.Users
{
        public class UserRefreshTokens
        {
                public Guid Id { get; set; }
                public Guid UserId { get; set; }
                public required string RefreshToken { get; set; }
                public DateTime ExpiryDate { get; set; }
                public ICollection<Token>? Token { get; set; }
        }
}
