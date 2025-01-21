namespace movieTickApi.Models
{
        public class UserProfile
        {
                public Guid Id { get; set; }
                public string Name { get; set; }
                public Guid UserId { get; set; }
                public DateTime CreateDatetime { get; set; }
                public DateTime ModifyDatetime { get; set; }
                public User User { get; set; }
        }
}
