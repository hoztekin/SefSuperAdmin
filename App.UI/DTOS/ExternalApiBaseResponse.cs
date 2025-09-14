namespace App.UI.DTOS
{
    public class ExternalApiBaseResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; }
        public int StatusCode { get; set; }
    }
}
