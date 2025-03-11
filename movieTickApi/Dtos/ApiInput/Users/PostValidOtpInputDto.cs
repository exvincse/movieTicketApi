namespace movieTickApi.Dtos.Input.Users
{
        public class PostValidInputDto
        {
                public required string Email { get; set; }
                public required string Otp { get; set; }
        }
}
