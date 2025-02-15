namespace movieTickApi.Models.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime CreateDatetime { get; set; }
        public DateTime ModifyDatetime { get; set; }
        public ICollection<UserProfile> UserProfile { get; set; }
    }
}
