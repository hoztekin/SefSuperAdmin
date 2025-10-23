using App.UI.Application.DTOS;
using App.UI.Application.Services;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    public class ExternalUserController(IExternalUserService externalUserService,
                                        IExternalApiService externalApiService,
                                        ISessionService sessionService,
                                        ILogger<ExternalUserController> logger) : Controller
    {
       

        // GET: ExternalUser/Index - External User listesini göster
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await externalUserService.GetUsersAsync();
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index", "Home");
                }
                return View(result.Data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Index yüklenirken hata");
                TempData["ErrorMessage"] = "Hata oluştu";
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

            var result = await externalUserService.CreateUserAsync(createUserDto);
            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        // PUT: ExternalUser/UpdateUser - External user güncelle
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateExternalUserDto updateUserDto)
        {
            if (updateUserDto == null || string.IsNullOrEmpty(updateUserDto.Id))
                return Json(new { success = false, message = "Geçersiz veri" });

            var result = await externalUserService.UpdateUserAsync(updateUserDto);
            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        // DELETE: ExternalUser/DeleteUser - External user sil
        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteDto deleteDto)
        {
            if (deleteDto == null || string.IsNullOrEmpty(deleteDto.Id))
                return Json(new { success = false, message = "Geçersiz ID" });

            var result = await externalUserService.DeleteUserAsync(deleteDto.Id);
            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        // GET: ExternalUser/GetUserById - Belirli bir external user'ı getir (AJAX için)
        [HttpGet]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var result = await externalUserService.GetUserByIdAsync(userId);
            return Json(new { success = result.IsSuccess, data = result.Data, message = result.Message });
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
                var result = await externalUserService.ChangeUserStatusAsync(userId, isActive);

                if (result.IsSuccess)
                {
                    logger.LogInformation("External kullanıcı durumu değiştirildi: UserId: {UserId}, IsActive: {IsActive}", userId, isActive);
                    return Json(new { success = true, message = "External kullanıcı durumu başarıyla değiştirildi" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "External kullanıcı durumu değiştirilirken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "External kullanıcı durumu değiştirilirken bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                var selectedMachine = sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return Json(new { success = false, message = "Seçili makine bulunamadı" });
                }

                var healthResponse = await externalApiService.CheckHealthAsync(selectedMachine.ApiAddress);

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
                logger.LogError(ex, "External API bağlantı kontrolünde hata oluştu");
                return Json(new { success = false, message = "Bağlantı kontrol edilemedi: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var result = await externalUserService.ChangePasswordAsync(changePasswordDto);
            return Json(new { success = result.IsSuccess, message = result.Message });
        }
    }
}
