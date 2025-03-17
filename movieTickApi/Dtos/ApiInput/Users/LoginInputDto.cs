namespace movieTickApi.Dtos.Input.Users
{
        public class LoginInputDto
        {
                public required string Email { get; set; }
                public required string Password { get; set; }
        }
}
