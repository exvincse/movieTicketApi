namespace movieTickApi.Dtos.Output.Users
{
        public class RequestResultOutputDto<T>
        {
                public int StatusCode { get; set; }
                public required string Message { get; set; }
                public required T Result { get; set; }
        }
}
