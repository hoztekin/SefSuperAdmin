using App.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    public class ExternalApiBaseController : Controller
    {
        protected readonly IExternalApiService _externalApiService;
        protected readonly ISessionService _sessionService;
        protected readonly ILogger _logger;

        protected ExternalApiBaseController(
             IExternalApiService externalApiService,
             ISessionService sessionService,
             ILogger logger)
        {
            _externalApiService = externalApiService;
            _sessionService = sessionService;
            _logger = logger;
        }

        /// <summary>
        /// Seçili makinenin API adresini ve geçerli token'ını döner
        /// </summary>
        protected (string apiAddress, string token)? GetSelectedMachineApiInfo()
        {
            var selectedMachine = _sessionService.GetSelectedMachine();
            if (selectedMachine == null)
            {
                _logger.LogWarning("Seçili makine bulunamadı");
                return null;
            }

            var tokenInfo = _sessionService.GetExternalApiToken(selectedMachine.ApiAddress);
            if (tokenInfo == null || tokenInfo.IsExpired)
            {
                _logger.LogWarning("Geçerli token bulunamadı: {ApiAddress}", selectedMachine.ApiAddress);
                return null;
            }

            return (selectedMachine.ApiAddress, tokenInfo.AccessToken);
        }

        /// <summary>
        /// Uzaktaki API'ye GET isteği atar
        /// </summary>
        protected async Task<T> GetFromExternalApiAsync<T>(string endpoint)
        {
            var apiInfo = GetSelectedMachineApiInfo();
            if (apiInfo == null)
            {
                throw new InvalidOperationException("Seçili makine veya geçerli token bulunamadı");
            }

            return await _externalApiService.GetWithTokenAsync<T>(apiInfo.Value.apiAddress, endpoint, apiInfo.Value.token);
        }

        /// <summary>
        /// Uzaktaki API'ye POST isteği atar
        /// </summary>
        protected async Task<T> PostToExternalApiAsync<T>(string endpoint, object data)
        {
            var apiInfo = GetSelectedMachineApiInfo();
            if (apiInfo == null)
            {
                throw new InvalidOperationException("Seçili makine veya geçerli token bulunamadı");
            }

            return await _externalApiService.PostWithTokenAsync<T>(apiInfo.Value.apiAddress, endpoint, data, apiInfo.Value.token);
        }

        /// <summary>
        /// API bağlantısı gerektiren işlemler için standard error response
        /// </summary>
        protected IActionResult ApiConnectionError(string message = "API bağlantısı gerekli")
        {
            return Json(new { success = false, message = message, requiresMachineSelection = true });
        }
    }
}
