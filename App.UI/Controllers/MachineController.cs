using App.UI.Application.DTOS;
using App.UI.Helper;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Http;
using App.UI.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    [Authorize]
    public class MachineController : Controller
    {
        private readonly ILogger<MachineController> _logger;
        private readonly IApiService _apiService;
        private readonly IExternalApiService _externalApiService;

        public MachineController(ILogger<MachineController> logger, IApiService apiService, IExternalApiService externalApiService)
        {
            _logger = logger;
            _apiService = apiService;
            _externalApiService = externalApiService;
        }

        // Makine listesi
        public async Task<IActionResult> Index()
        {
            try
            {
                var machines = await _apiService.GetAsync<List<MachineListViewModel>>("api/v1/Machine");
                return View(machines ?? new List<MachineListViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makineler yüklenirken hata oluştu");
                this.SetErrorMessage("Makineler yüklenirken hata oluştu");
                return View(new List<MachineListViewModel>());
            }
        }

        // Yeni makine oluşturma formu
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateMachineViewModel());
        }

        // Yeni makine oluşturma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMachineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateMachineViewModel>("api/v1/Machine", model);

                if (result != null)
                {
                    this.SetSuccessMessage("Makine başarıyla oluşturuldu");
                    return RedirectToAction(nameof(Index));
                }

                this.SetErrorMessage("Makine oluşturulamadı");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine oluşturulurken hata oluştu");
                this.SetErrorMessage("Makine oluşturulurken hata oluştu");
                return View(model);
            }
        }

        // Makine düzenleme formu
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var machine = await _apiService.GetAsync<MachineViewModel>($"api/v1/Machine/{id}");

                if (machine == null)
                {
                    this.SetErrorMessage("Makine bulunamadı");
                    return RedirectToAction(nameof(Index));
                }

                var model = new UpdateMachineViewModel
                {
                    Id = machine.Id,
                    BranchId = machine.BranchId,
                    BranchName = machine.BranchName,
                    ApiAddress = machine.ApiAddress,
                    Code = machine.Code
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine bilgisi alınırken hata oluştu. ID: {Id}", id);
                this.SetErrorMessage("Makine bilgisi alınamadı");
                return RedirectToAction(nameof(Index));
            }
        }

        // Makine düzenleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateMachineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.PutAsync<bool>($"api/v1/Machine/{model.Id}", model);

                if (success)
                {
                    this.SetSuccessMessage("Makine başarıyla güncellendi");
                    return RedirectToAction(nameof(Index));
                }

                this.SetErrorMessage("Makine güncellenemedi");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine güncellenirken hata oluştu. ID: {Id}", model.Id);
                this.SetErrorMessage("Makine güncellenirken hata oluştu");
                return View(model);
            }
        }

        // Makine detayı
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var machine = await _apiService.GetAsync<MachineViewModel>($"api/v1/Machine/{id}");

                if (machine == null)
                {
                    this.SetErrorMessage("Makine bulunamadı");
                    return RedirectToAction(nameof(Index));
                }

                return View(machine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine detayı alınırken hata oluştu. ID: {Id}", id);
                this.SetErrorMessage("Makine detayı alınamadı");
                return RedirectToAction(nameof(Index));
            }
        }

        // Makine silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteAsync<bool>($"api/v1/Machine/{id}");

                if (success)
                {
                    this.SetSuccessMessage("Makine başarıyla silindi");
                }
                else
                {
                    this.SetErrorMessage("Makine silinemedi");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine silinirken hata oluştu. ID: {Id}", id);
                this.SetErrorMessage("Makine silinirken hata oluştu");
            }

            return RedirectToAction(nameof(Index));
        }

        // Makine durumu değiştirme
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var success = await _apiService.PutAsync<bool>($"api/v1/Machine/{id}/status?isActive={isActive}", null);

                if (success)
                {
                    return Json(new { success = true, message = "Makine durumu güncellendi" });
                }

                return Json(new { success = false, message = "Makine durumu güncellenemedi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine durumu güncellenirken hata oluştu. ID: {Id}", id);
                return Json(new { success = false, message = "Makine durumu güncellenirken hata oluştu" });
            }
        }

        // API bağlantı testi
        [HttpPost]
        public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.ApiAddress))
                {
                    return Json(new
                    {
                        success = false,
                        connected = false,
                        message = "API adresi gerekli",
                        responseTime = 0
                    });
                }

                _logger.LogInformation("API health check başlatıldı: {ApiAddress}", request.ApiAddress);

                // ✅ YENİ: Direkt hedef API'nin health endpoint'ine istek at
                var healthResponse = await _externalApiService.CheckHealthAsync(request.ApiAddress);

                if (healthResponse.IsHealthy)
                {
                    _logger.LogInformation("API health check başarılı: {ApiAddress} - {ResponseTime}ms",
                        request.ApiAddress, healthResponse.ResponseTime);

                    return Json(new
                    {
                        success = true,
                        connected = true,
                        message = $"API başarıyla yanıt verdi ({healthResponse.ResponseTime}ms)",
                        responseTime = healthResponse.ResponseTime,
                        details = new
                        {
                            healthy = true,
                            endpoint = $"{request.ApiAddress.TrimEnd('/')}/health",
                            status = healthResponse.Message ?? "Healthy"
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("API health check başarısız: {ApiAddress} - {Message}",
                        request.ApiAddress, healthResponse.Message);

                    return Json(new
                    {
                        success = true, // İstek başarılı ama API sağlıksız
                        connected = false,
                        message = $"API yanıt vermiyor: {healthResponse.Message}",
                        responseTime = healthResponse.ResponseTime,
                        details = new
                        {
                            healthy = false,
                            endpoint = $"{request.ApiAddress.TrimEnd('/')}/health",
                            status = healthResponse.Message ?? "Unhealthy"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API health check sırasında hata oluştu. ApiAddress: {ApiAddress}", request?.ApiAddress);

                return Json(new
                {
                    success = false,
                    connected = false,
                    message = "Bağlantı testi sırasında beklenmeyen bir hata oluştu",
                    responseTime = 0,
                    details = new
                    {
                        healthy = false,
                        endpoint = $"{request?.ApiAddress?.TrimEnd('/')}/health",
                        error = ex.Message
                    }
                });
            }
        }

        // AJAX - Kod benzersizlik kontrolü
        [HttpGet]
        public async Task<IActionResult> CheckCodeExists(string code, int? excludeId = null)
        {
            try
            {
                var exists = await _apiService.GetAsync<bool>($"api/v1/Machine/check-code?code={code}&excludeId={excludeId}");
                return Json(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kod kontrolü sırasında hata oluştu. Code: {Code}", code);
                return Json(new { exists = false });
            }
        }

        // AJAX - Şube ID benzersizlik kontrolü  
        [HttpGet]
        public async Task<IActionResult> CheckBranchIdExists(string branchId, int? excludeId = null)
        {
            try
            {
                var exists = await _apiService.GetAsync<bool>($"api/v1/Machine/check-branch-id?branchId={branchId}&excludeId={excludeId}");
                return Json(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şube ID kontrolü sırasında hata oluştu. BranchId: {BranchId}", branchId);
                return Json(new { exists = false });
            }
        }
    }
}