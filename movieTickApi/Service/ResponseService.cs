using movieTickApi.Dtos.Output.Users;

namespace movieTickApi.Service
{
    public class ResponseService
        {
                public RequestResultOutputDto<T> RequestResult<T>(RequestResultOutputDto<object> param)
                {
                        return new RequestResultOutputDto<T>
                        {
                                StatusCode = param.StatusCode,
                                Message = param.Message,
                                Result = (T)param.Result
                        };
                }
        }
}
