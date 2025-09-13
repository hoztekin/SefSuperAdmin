using App.UI.DTOS;
using App.UI.Helper;
using App.UI.Services;
using App.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.UI.Controllers
{
    [Authorize]
    public class MachineController : Controller
    {
        private readonly ILogger<MachineController> _logger;
        private readonly IApiService _apiService;

        public MachineController(ILogger<MachineController> logger, IApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
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
                var result = await _apiService.GetAsync<bool>($"api/v1/Machine/test-connection?apiAddress={Uri.EscapeDataString(request.ApiAddress)}");

                return Json(new
                {
                    success = true,
                    connected = result,
                    message = result ? "API adresine başarıyla bağlanıldı" : "API adresine bağlanılamadı"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API bağlantı testi sırasında hata oluştu. ApiAddress: {ApiAddress}", request.ApiAddress);
                return Json(new
                {
                    success = false,
                    connected = false,
                    message = "Bağlantı testi sırasında hata oluştu"
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