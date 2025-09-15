using App.UI.Application.DTOS;

namespace App.UI.Presentation.ViewModels
{
    public class ApiOperationsViewModel
    {
        public SelectedMachineInfo SelectedMachine { get; set; }
        public ExternalApiHealthResponse HealthStatus { get; set; }
        public List<ApiEndpoint> AvailableEndpoints { get; set; } = new();
    }

    public class ApiEndpoint
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
        public string Description { get; set; }
    }
}
