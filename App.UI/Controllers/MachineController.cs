using App.UI.Application.DTOS;
using App.UI.Application.Services;
using App.UI.Helper;
using App.UI.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class MachineController(IMachineAppService machineAppService) : Controller
    {
        // Ana sayfa
        public async Task<IActionResult> Index()
        {
            var result = await machineAppService.GetAllAsync();

            if (!result.IsSuccess)
            {
                this.SetErrorMessage(result.ErrorMessage?.FirstOrDefault() ?? "Makineler yüklenemedi");
                return View(new List<MachineListViewModel>());
            }

            return View(result.Data ?? new List<MachineListViewModel>());
        }

        // Yeni makine oluşturma (AJAX)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMachineViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                    return Json(new { success = false, message = "Form validation failed", errors = errors });
                }

                var result = await machineAppService.CreateAsync(model);

                if (result.IsSuccess)
                {
                    return Json(new { success = true, message = "Makine başarıyla oluşturuldu" });
                }

                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage?.FirstOrDefault() ?? "Makine oluşturulamadı",
                    errors = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Beklenmeyen bir hata oluştu" });
            }
        }

        // Makine bilgilerini getir (AJAX için)
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await machineAppService.GetByIdAsync(id);

                if (result.IsSuccess)
                {
                    return Json(new { success = true, data = result.Data });
                }

                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage?.FirstOrDefault() ?? "Makine bulunamadı"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Beklenmeyen bir hata oluştu" });
            }
        }

        // Makine düzenleme (AJAX)
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateMachineViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                    return Json(new { success = false, message = "Form validation failed", errors = errors });
                }

                var result = await machineAppService.UpdateAsync(model);

                if (result.IsSuccess)
                {
                    return Json(new { success = true, message = "Makine başarıyla güncellendi" });
                }

                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage?.FirstOrDefault() ?? "Makine güncellenemedi",
                    errors = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Beklenmeyen bir hata oluştu" });
            }
        }

        // Makine silme (AJAX)
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] DeleteMachineRequest request)
        {
            try
            {
                if (request?.Id == null || request.Id <= 0)
                {
                    return Json(new { success = false, message = "Geçersiz makine ID'si" });
                }

                var result = await machineAppService.DeleteAsync(request.Id);

                if (result.IsSuccess)
                {
                    return Json(new { success = true, message = "Makine başarıyla silindi" });
                }

                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage?.FirstOrDefault() ?? "Makine silinemedi"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Beklenmeyen bir hata oluştu" });
            }
        }

        // AJAX ile makine durumu değiştirme
        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest request)
        {
            try
            {
                if (request == null || request.Id <= 0)
                {
                    return Json(new { success = false, message = "Geçersiz istek" });
                }

                var result = await machineAppService.SetActiveStatusAsync(request.Id, request.IsActive);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = result.ErrorMessage?.FirstOrDefault() ?? "Durum başarıyla güncellendi",
                        isActive = request.IsActive
                    });
                }

                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage?.FirstOrDefault() ?? "Durum güncellenemedi"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Beklenmeyen bir hata oluştu"
                });
            }
        }

        // AJAX ile API bağlantı testi
        [HttpPost]
        public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ApiAddress))
                {
                    return Json(new { success = false, message = "API adresi gereklidir" });
                }

                var result = await machineAppService.TestApiConnectionAsync(request.ApiAddress);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = result.Data,
                        message = result.Data ? "Bağlantı başarılı" : "Bağlantı başarısız",
                        connected = result.Data
                    });
                }

                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage?.FirstOrDefault() ?? "Bağlantı testi yapılamadı"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Bağlantı testi sırasında hata oluştu"
                });
            }
        }
    }
}