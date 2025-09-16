using System.Net;
using System.Text.Json.Serialization;

namespace App.UI.Application.DTOS
{
    public class ServiceResult<T>
    {
        public T Data { get; set; }
        public List<string> ErrorMessage { get; set; }


        [JsonIgnore]
        public bool IsSuccess => ErrorMessage == null || ErrorMessage.Count == 0;

        [JsonIgnore]
        public bool IsFail => !IsSuccess;

        public int StatusCode { get; set; }

        [JsonIgnore]
        public string Message => ErrorMessage != null && ErrorMessage.Count > 0
            ? string.Join(", ", ErrorMessage)
            : null;

        public static ServiceResult<T> Fail(List<string> errorMessage, HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            return new ServiceResult<T>()
            {
                ErrorMessage = errorMessage,
                StatusCode = (int)status
            };
        }

        public static ServiceResult<T> Fail(string errorMessage, HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            return new ServiceResult<T>()
            {
                ErrorMessage = new List<string> { errorMessage },
                StatusCode = (int)status
            };
        }

        public static ServiceResult<T> Success(T data, HttpStatusCode status = HttpStatusCode.OK)
        {
            return new ServiceResult<T>()
            {
                Data = data,
                StatusCode = (int)status,
                ErrorMessage = null
            };
        }

        [JsonIgnore]
        public HttpStatusCode Status
        {
            get => (HttpStatusCode)StatusCode;
            set => StatusCode = (int)value;
        }
    }

    public class ServiceResult
    {
        public List<string> ErrorMessage { get; set; }

        [JsonIgnore]
        public bool IsSuccess => ErrorMessage == null || ErrorMessage.Count == 0;

        [JsonIgnore]
        public bool IsFail => !IsSuccess;

        public int StatusCode { get; set; }

        [JsonIgnore]
        public HttpStatusCode Status
        {
            get => (HttpStatusCode)StatusCode;
            set => StatusCode = (int)value;
        }

        [JsonIgnore]
        public string Message => ErrorMessage != null && ErrorMessage.Count > 0
            ? string.Join(", ", ErrorMessage)
            : null;



        public static ServiceResult Fail(List<string> errorMessage, HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            return new ServiceResult()
            {
                ErrorMessage = errorMessage,
                StatusCode = (int)status
            };
        }

        public static ServiceResult Fail(string errorMessage, HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            return new ServiceResult()
            {
                ErrorMessage = new List<string> { errorMessage },
                StatusCode = (int)status
            };
        }

        public static ServiceResult Success(HttpStatusCode status = HttpStatusCode.OK)
        {
            return new ServiceResult()
            {
                StatusCode = (int)status,
                ErrorMessage = null
            };
        }

    }
}
