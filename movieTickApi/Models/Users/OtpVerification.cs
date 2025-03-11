namespace movieTickApi.Models.Users
{
        public class OtpVerification
        {
                public required Guid Id { get; set; }
                public required string Email { get; set; }
                public required string Otp { get; set; }
                public DateTime ExpirationTime { get; set; }
                public bool IsUsed { get; set; }
                public DateTime CreatedAt { get; set; }
                public DateTime UpdatedAt { get; set; }
        }
}
