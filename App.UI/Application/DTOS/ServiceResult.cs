using System.Text.Json.Serialization;

namespace App.UI.Application.DTOS
{
    public class ServiceResult<T>
    {
        public T Data { get; set; }
        public List<string> ErrorMessage { get; set; }

        [JsonIgnore]
        public bool Success => ErrorMessage == null || ErrorMessage.Count == 0;

        public int StatusCode { get; set; }

        [JsonIgnore]
        public string Message => ErrorMessage != null && ErrorMessage.Count > 0
            ? string.Join(", ", ErrorMessage)
            : null;
    }

}
