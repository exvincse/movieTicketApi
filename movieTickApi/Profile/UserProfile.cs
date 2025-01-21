using movieTickApi.Dtos;
using movieTickApi.Models;

namespace movieTickApi.Profile
{
        public class UserProfile: AutoMapper.Profile
        {
                public UserProfile()
                {
                        CreateMap<User, UserDto>();
                }
        }
}