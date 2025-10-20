using App.Services.Users.Create;
using App.Services.Users.Update;
using App.UI.Application.DTOS;
using App.UI.Application.Services;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    public class ExternalUserController : Controller
    {
        private readonly IExternalApiService _externalApiService;
        private readonly IExternalUserService _externalUserService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<ExternalUserController> _logger;

        public ExternalUserController(
            IExternalUserService externalUserService,
            IExternalApiService externalApiService,
            ISessionService sessionService,
            ILogger<ExternalUserController> logger)
        {
            _externalUserService = externalUserService;
            _sessionService = sessionService;
            _externalApiService = externalApiService;
            _logger = logger;
        }

        // GET: ExternalUser/Index - External User listesini göster
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Sadece Service çağrısı - business logic yok
                var result = await _externalUserService.GetUsersAsync();

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }

                var selectedMachine = _sessionService.GetSelectedMachine();
                ViewData["SelectedMachine"] = selectedMachine;
                ViewData["Title"] = $"External Kullanıcı Yönetimi - {selectedMachine?.BranchName}";

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExternalUser Index yüklenirken hata oluştu");
                TempData["ErrorMessage"] = "External kullanıcı listesi yüklenirken bir hata oluştu.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: ExternalUser/CreateUser - Yeni external user oluştur
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateExternalUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Validasyon hatası", errors = errors });
            }

            try
            {
                var result = await _externalUserService.CreateUserAsync(createUserDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("External kullanıcı oluşturuldu: {UserName}", createUserDto.UserName);
                    return Json(new { success = true, message = "External kullanıcı başarıyla oluşturuldu" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External kullanıcı oluşturulurken hata oluştu. UserName: {UserName}", createUserDto.UserName);
                return Json(new { success = false, message = "External kullanıcı oluşturulurken bir hata oluştu: " + ex.Message });
            }
        }

        // PUT: ExternalUser/UpdateUser - External user güncelle
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateExternalUserDto updateUserDto)
        {
            if (updateUserDto == null)
            {
                return Json(new { success = false, message = "Geçersiz veri gönderildi" });
            }

            if (string.IsNullOrEmpty(updateUserDto.Id))
            {
                return Json(new { success = false, message = "Kullanıcı ID'si gereklidir" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Validasyon hatası", errors = errors });
            }

            try
            {
                var result = await _externalUserService.UpdateUserAsync(updateUserDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("External kullanıcı güncellendi: UserId: {UserId}", updateUserDto.Id);
                    return Json(new { success = true, message = "External kullanıcı başarıyla güncellendi" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External kullanıcı güncellenirken hata oluştu. UserId: {UserId}", updateUserDto.Id);
                return Json(new { success = false, message = "External kullanıcı güncellenirken bir hata oluştu: " + ex.Message });
            }
        }

        // DELETE: ExternalUser/DeleteUser - External user sil
        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteDto deleteDto)
        {
            if (deleteDto == null || string.IsNullOrEmpty(deleteDto.Id))
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı ID'si" });
            }

            try
            {
                var result = await _externalUserService.DeleteUserAsync(deleteDto.Id);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("External kullanıcı silindi: UserId: {UserId}", deleteDto.Id);
                    return Json(new { success = true, message = "External kullanıcı başarıyla silindi" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External kullanıcı silinirken hata oluştu. UserId: {UserId}", deleteDto.Id);
                return Json(new { success = false, message = "External kullanıcı silinirken bir hata oluştu: " + ex.Message });
            }
        }

        // GET: ExternalUser/GetUserById - Belirli bir external user'ı getir (AJAX için)
        [HttpGet]
        public async Task<IActionResult> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı ID'si" });
            }

            try
            {
                var result = await _externalUserService.GetUserByIdAsync(userId);

                if (result.IsSuccess)
                {
                    return Json(new { success = true, data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External kullanıcı detayı alınırken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "External kullanıcı detayı alınırken bir hata oluştu: " + ex.Message });
            }
        }

        // PUT: ExternalUser/ChangeUserStatus - External user durumu değiştir
        [HttpPut]
        public async Task<IActionResult> ChangeUserStatus(string userId, [FromBody] bool isActive)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı ID'si" });
            }

            try
            {
                var result = await _externalUserService.ChangeUserStatusAsync(userId, isActive);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("External kullanıcı durumu değiştirildi: UserId: {UserId}, IsActive: {IsActive}", userId, isActive);
                    return Json(new { success = true, message = "External kullanıcı durumu başarıyla değiştirildi" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External kullanıcı durumu değiştirilirken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "External kullanıcı durumu değiştirilirken bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                var selectedMachine = _sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return Json(new { success = false, message = "Seçili makine bulunamadı" });
                }

                var healthResponse = await _externalApiService.CheckHealthAsync(selectedMachine.ApiAddress);

                return Json(new
                {
                    success = healthResponse.IsHealthy,
                    message = healthResponse.Message,
                    data = new
                    {
                        apiAddress = selectedMachine.ApiAddress,
                        branchName = selectedMachine.BranchName,
                        isHealthy = healthResponse.IsHealthy
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API bağlantı kontrolünde hata oluştu");
                return Json(new { success = false, message = "Bağlantı kontrol edilemedi: " + ex.Message });
            }
        }
    }
}
