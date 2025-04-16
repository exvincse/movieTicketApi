namespace movieTickApi.Models.Users
{
        public class User
        {
                public Guid Id { get; set; }
                public int UserNo { get; set; }
                public string? Email { get; set; }
                public string? Password { get; set; }
                public string? GoogleSub { get; set; }
                public DateTime CreateDateTime { get; set; }
                public DateTime ModifyDateTime { get; set; }
                public ICollection<UserProfile> UserProfile { get; set; }
        }
}
