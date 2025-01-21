namespace movieTickApi.Models
{
        public class UserRefreshTokens
        {
                public Guid Id { get; set; }
                public Guid UserId { get; set; }
                public string RefreshToken { get; set; }
                public DateTime ExpiryDate { get; set; }
        }
}
