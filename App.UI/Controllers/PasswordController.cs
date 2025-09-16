using App.UI.Application.Services;
using App.UI.Helper;
using App.UI.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.UI.Controllers
{
    [Authorize]
    public class PasswordController(IAccountService accountService) : Controller
    {
        public IActionResult PasswordChange()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PasswordChange(PasswordChangeViewModel passwordChangeViewModel)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = "Validasyon hatası", errors = errors });
                }
                return View(passwordChangeViewModel);
            }

            try
            {
                passwordChangeViewModel.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


                var result = await accountService.PasswordChangeAsync(passwordChangeViewModel);

                if (result.IsSuccess) 
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Parolanız başarıyla değiştirildi" });
                    }

                    this.SetSuccessMessage("Parolanız başarıyla değiştirildi");
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = result.ErrorMessage.FirstOrDefault() ?? "Şifre değiştirilemedi" });
                    }

                    ModelState.AddModelError("", result.ErrorMessage.FirstOrDefault() ?? "Şifre değiştirilemedi");
                    this.SetErrorMessage(result.ErrorMessage.FirstOrDefault() ?? "Parolanız değiştirilirken bir hata oluştu");
                    return View(passwordChangeViewModel);
                }
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Parola değiştirilirken bir hata oluştu: {ex.Message}"
                    });
                }

                ModelState.AddModelError("", $"Kayıt işlemi sırasında hata: {ex.Message}");
                this.SetErrorMessage($"Parolanız değiştirilirken bir hata oluştu");
                return View(passwordChangeViewModel);
            }
        }
    }
}