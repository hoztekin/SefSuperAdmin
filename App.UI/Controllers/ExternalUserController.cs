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

        //// POST: ExternalUser/CreateUser - Yeni external user oluştur
        //[HttpPost]
        //public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        //        return Json(new { success = false, message = "Validasyon hatası", errors = errors });
        //    }

        //    try
        //    {
        //        // Seçili makine kontrolü
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return Json(new { success = false, message = "Seçili makine bulunamadı" });
        //        }

        //        // Token kontrolü
        //        var token = _sessionService.GetMachineApiToken();
        //        if (string.IsNullOrEmpty(token))
        //        {
        //            // Token yenilemeyi dene
        //            var loginResponse = await _externalApiService.LoginAsync(selectedMachine.ApiAddress, "SystemAdmin", "1234");
        //            if (!loginResponse.Success)
        //            {
        //                return Json(new { success = false, message = "External API'ye bağlanılamadı" });
        //            }

        //            _sessionService.SaveMachineApiToken(selectedMachine.ApiAddress, loginResponse.AccessToken,
        //                loginResponse.ExpiresAt != default ? loginResponse.ExpiresAt : DateTime.Now.AddHours(1));
        //            token = loginResponse.AccessToken;
        //        }

        //        // External API'ye kullanıcı oluşturma isteği gönder
        //        var result = await _externalApiService.PostWithTokenAsync<object>(
        //            selectedMachine.ApiAddress,
        //            "api/user",
        //            createUserDto,
        //            token
        //        );

        //        if (result != null)
        //        {
        //            _logger.LogInformation("External kullanıcı oluşturuldu: {UserName} - {ApiAddress}",
        //                createUserDto.UserName, selectedMachine.ApiAddress);
        //            return Json(new { success = true, message = "External kullanıcı başarıyla oluşturuldu" });
        //        }

        //        return Json(new { success = false, message = "External kullanıcı oluşturulamadı" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "External kullanıcı oluşturulurken hata oluştu. UserName: {UserName}", createUserDto.UserName);
        //        return Json(new { success = false, message = "External kullanıcı oluşturulurken bir hata oluştu: " + ex.Message });
        //    }
        //}

        //// PUT: ExternalUser/UpdateUser - External user güncelle
        //[HttpPut]
        //public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto updateUserDto)
        //{
        //    if (string.IsNullOrEmpty(userId) || updateUserDto == null)
        //    {
        //        return Json(new { success = false, message = "Geçersiz veri gönderildi" });
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        //        return Json(new { success = false, message = "Validasyon hatası", errors = errors });
        //    }

        //    try
        //    {
        //        // Seçili makine kontrolü
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return Json(new { success = false, message = "Seçili makine bulunamadı" });
        //        }

        //        // Token kontrolü
        //        var token = _sessionService.GetMachineApiToken();
        //        if (string.IsNullOrEmpty(token))
        //        {
        //            return Json(new { success = false, message = "Token bulunamadı, sayfayı yenileyin" });
        //        }

        //        // External API'ye güncelleme isteği gönder
        //        var result = await _externalApiService.PutWithTokenAsync<object>(
        //            selectedMachine.ApiAddress,
        //            $"api/user/{userId}",
        //            updateUserDto,
        //            token
        //        );

        //        if (result != null)
        //        {
        //            _logger.LogInformation("External kullanıcı güncellendi: UserId: {UserId} - {ApiAddress}",
        //                userId, selectedMachine.ApiAddress);
        //            return Json(new { success = true, message = "External kullanıcı başarıyla güncellendi" });
        //        }

        //        return Json(new { success = false, message = "External kullanıcı güncellenemedi" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "External kullanıcı güncellenirken hata oluştu. UserId: {UserId}", userId);
        //        return Json(new { success = false, message = "External kullanıcı güncellenirken bir hata oluştu: " + ex.Message });
        //    }
        //}

        //// DELETE: ExternalUser/DeleteUser - External user sil
        //[HttpDelete]
        //public async Task<IActionResult> DeleteUser(string userId)
        //{
        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Json(new { success = false, message = "Geçersiz kullanıcı ID'si" });
        //    }

        //    try
        //    {
        //        // Seçili makine kontrolü
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return Json(new { success = false, message = "Seçili makine bulunamadı" });
        //        }

        //        // Token kontrolü
        //        var token = _sessionService.GetMachineApiToken();
        //        if (string.IsNullOrEmpty(token))
        //        {
        //            return Json(new { success = false, message = "Token bulunamadı, sayfayı yenileyin" });
        //        }

        //        // External API'ye silme isteği gönder
        //        var result = await _externalApiService.DeleteWithTokenAsync<object>(
        //            selectedMachine.ApiAddress,
        //            $"api/user/{userId}",
        //            token
        //        );

        //        if (result != null)
        //        {
        //            _logger.LogInformation("External kullanıcı silindi: UserId: {UserId} - {ApiAddress}",
        //                userId, selectedMachine.ApiAddress);
        //            return Json(new { success = true, message = "External kullanıcı başarıyla silindi" });
        //        }

        //        return Json(new { success = false, message = "External kullanıcı silinemedi" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "External kullanıcı silinirken hata oluştu. UserId: {UserId}", userId);
        //        return Json(new { success = false, message = "External kullanıcı silinirken bir hata oluştu: " + ex.Message });
        //    }
        //}

        //// GET: ExternalUser/GetUserById - Belirli bir external user'ı getir (AJAX için)
        //[HttpGet]
        //public async Task<IActionResult> GetUserById(string userId)
        //{
        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Json(new { success = false, message = "Geçersiz kullanıcı ID'si" });
        //    }

        //    try
        //    {
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return Json(new { success = false, message = "Seçili makine bulunamadı" });
        //        }

        //        var token = _sessionService.GetMachineApiToken();
        //        if (string.IsNullOrEmpty(token))
        //        {
        //            return Json(new { success = false, message = "Token bulunamadı" });
        //        }

        //        var user = await _externalApiService.GetWithTokenAsync<ExternalUserDto>(
        //            selectedMachine.ApiAddress,
        //            $"api/user/{userId}",
        //            token
        //        );

        //        if (user != null)
        //        {
        //            return Json(new { success = true, data = user });
        //        }

        //        return Json(new { success = false, message = "Kullanıcı bulunamadı" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "External kullanıcı detayı alınırken hata oluştu. UserId: {UserId}", userId);
        //        return Json(new { success = false, message = "Kullanıcı detayları yüklenemedi" });
        //    }
        //}

        // GET: ExternalUser/CheckConnection - Bağlantı durumu kontrolü (AJAX için)
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
