using App.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    [Authorize]
    public class ExternalOperationsController : ExternalApiBaseController
    {
        public ExternalOperationsController(
            IExternalApiService externalApiService,
            ISessionService sessionService,
            ILogger<ExternalOperationsController> logger)
            : base(externalApiService, sessionService, logger)
        {
        }

        // Örnek: Uzaktaki API'den veri çekme
        [HttpGet]
        public async Task<IActionResult> GetDataFromSelectedMachine()
        {
            try
            {
                var apiInfo = GetSelectedMachineApiInfo();
                if (apiInfo == null)
                {
                    return ApiConnectionError("Önce bir makine seçmeniz gerekiyor");
                }

                // Uzaktaki API'den veri çek - endpoint uzaktaki API'nin formatına göre ayarlanmalı
                var data = await GetFromExternalApiAsync<dynamic>("api/v1/data");

                if (data != null)
                {
                    return Json(new { success = true, data = data });
                }

                return Json(new { success = false, message = "Veri alınamadı" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "API bağlantı problemi");
                return ApiConnectionError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API veri çekme hatası");
                return Json(new { success = false, message = "Veri çekerken hata oluştu" });
            }
        }

        // Örnek: Uzaktaki API'ye veri gönderme
        [HttpPost]
        public async Task<IActionResult> SendDataToSelectedMachine([FromBody] dynamic requestData)
        {
            try
            {
                var apiInfo = GetSelectedMachineApiInfo();
                if (apiInfo == null)
                {
                    return ApiConnectionError("Önce bir makine seçmeniz gerekiyor");
                }

                // Uzaktaki API'ye veri gönder
                var result = await PostToExternalApiAsync<dynamic>("api/v1/process", requestData);

                if (result != null)
                {
                    return Json(new { success = true, result = result });
                }

                return Json(new { success = false, message = "İşlem tamamlanamadı" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "API bağlantı problemi");
                return ApiConnectionError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API veri gönderme hatası");
                return Json(new { success = false, message = "İşlem sırasında hata oluştu" });
            }
        }

        // Token durumu kontrol et
        [HttpGet]
        public IActionResult CheckTokenStatus()
        {
            try
            {
                var selectedMachine = _sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return Json(new { hasSelectedMachine = false });
                }

                var tokenInfo = _sessionService.GetExternalApiToken(selectedMachine.ApiAddress);

                return Json(new
                {
                    hasSelectedMachine = true,
                    machineInfo = new
                    {
                        branchName = selectedMachine.BranchName,
                        apiAddress = selectedMachine.ApiAddress
                    },
                    token = new
                    {
                        hasValidToken = tokenInfo != null && !tokenInfo.IsExpired,
                        expiresAt = tokenInfo?.ExpiresAt,
                        isExpired = tokenInfo?.IsExpired ?? true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token durumu kontrol hatası");
                return Json(new { success = false, message = "Token durumu kontrol edilemedi" });
            }
        }
    }
}
