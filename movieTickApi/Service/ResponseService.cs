using movieTickApi.Dtos.Output.Users;

namespace movieTickApi.Service
{
        public class ResponseService
        {
                public RequestResultOutputDto<T> ApiRequestResult<T>(int statusCode, string message, T result = default)
                {
                        return new RequestResultOutputDto<T>
                        {
                                StatusCode = statusCode,
                                Message = message,
                                Result = result
                        };
                }

                public RequestResultOutputDto<T> RequestResult<T>(RequestResultOutputDto<T> param)
                {
                        return new RequestResultOutputDto<T>
                        {
                                StatusCode = param.StatusCode,
                                Message = param.Message,
                                Result = param.Result
                        };
                }
        }
}
