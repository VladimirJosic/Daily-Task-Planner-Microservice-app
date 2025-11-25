using UserService.Common.ENUMs;

namespace UserService.Common
{
    public class ResultPackage<T>
    {
        public ResultStatus Status { get; set; }

        public string Message { get; set; }

        public T? Data { get; set; }

        public ResultPackage()
        {
            Status = ResultStatus.OK;
            Message = string.Empty;
        }

        public ResultPackage(T data, ResultStatus status = ResultStatus.OK, string message = "")
        {
            Data = data;
            Status = status;
            Message = message;
        }

        public ResultPackage(ResultStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}
