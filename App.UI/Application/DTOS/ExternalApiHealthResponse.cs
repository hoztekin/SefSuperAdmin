namespace App.UI.Application.DTOS
{
    public class ExternalApiHealthResponse
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; }
        public int ResponseTime { get; set; } // milliseconds
    }
}
