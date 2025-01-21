namespace movieTickApi.Dtos
{
        public class UserDto
        {
                public Guid Id { get; set; }
                public string Name { get; set; }
                public List<TestUserDto> TestName { get; set; }
        }

        public class TestUserDto
        {
                public Guid Id { get; set; }
                public string Name { get; set; }
                public Guid UserId { get; set; }
        }

        public class UpdateUserDto
        {
                public Guid Id { get; set; }
                public string Name { get; set; }
        }
}
