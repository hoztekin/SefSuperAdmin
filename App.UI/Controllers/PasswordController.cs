using App.UI.Helper;
using App.UI.Presentation.ViewModels;
using App.UI.Services;
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
                return View(passwordChangeViewModel);
            }
            try
            {
                passwordChangeViewModel.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await accountService.PasswordChangeAsync(passwordChangeViewModel);
                this.SetSuccessMessage("Parolanız başarıyla değiştirildi");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Kayıt işlemi sırasında hata: {ex.Message}");
                this.SetErrorMessage($"Parolanız değiştirilirken bir hata oluştu");
                return View(passwordChangeViewModel);
            }
        }
    }
}
