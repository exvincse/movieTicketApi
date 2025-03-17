namespace movieTickApi.Models.Users
{
        public class User
        {
                public Guid Id { get; set; }
                public int UserNo { get; set; }
                public required string Email { get; set; }
                public required string Password { get; set; }
                public DateTime CreateDateTime { get; set; }
                public DateTime ModifyDateTime { get; set; }
                public ICollection<UserProfile> UserProfile { get; set; }
        }
}
