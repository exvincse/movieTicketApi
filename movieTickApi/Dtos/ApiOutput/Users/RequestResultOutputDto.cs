namespace movieTickApi.Dtos.Output.Users
{
        public class RequestResultOutputDto<T>
        {
                public int StatusCode { get; set; }
                public string Message { get; set; }
                public T? Result { get; set; }
        }
}
