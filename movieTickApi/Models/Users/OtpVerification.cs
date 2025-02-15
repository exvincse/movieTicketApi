namespace movieTickApi.Models.Users
{
    public class OtpVerification
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Otp { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
