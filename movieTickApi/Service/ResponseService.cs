using movieTickApi.Dtos;

namespace movieTickApi.Service
{
        public class ResponseService
        {
                public RequestResultDto<T> RequestResult<T>(RequestResultDto<object> param)
                {
                        return new RequestResultDto<T>
                        {
                                StatusCode = param.StatusCode,
                                Message = param.Message,
                                Result = (T)param.Result
                        };
                }
        }
}
