namespace movieTickApi.Dtos
{
        public class RequestResultDto<T>
        {
                public string StatusCode { get; set; }
                public string Message { get; set; }
                public T Result { get; set; }
        }
}
