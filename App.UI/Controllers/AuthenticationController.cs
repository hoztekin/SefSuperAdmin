using App.Services.Authentications.DTOs;
using App.Services.Authentications.Login;
using App.Services.Users.Create;
using App.UI.Helper;
using App.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace App.UI.Controllers
{
    [AllowAnonymous]
    public class AuthenticationController(IApiService apiService, IAuthService authService) : Controller
    {

        public IActionResult Register()
        {

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(CreateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                var response = await apiService.PostAsync<CreateUserDto>("api/v1/user", model, false);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Kayıt işlemi sırasında hata: {ex.Message}");
                return View(model);
            }
        }


        [HttpGet]
        public IActionResult Login(string? ReturnUrl)
        {
            TempData["ReturnUrl"] = ReturnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model, string? ReturnUrl = null)
        {

            if (!ModelState.IsValid)
            {
                TempData["ReturnUrl"] = ReturnUrl;
                return View(model);
            }

            try
            {
                var tokenService = HttpContext.RequestServices.GetRequiredService<ITokenService>();

                if (await tokenService.LoginAsync(model.UserName, model.Password))
                {
                    var session = SessionManager.GetSession();
                    if (session != null)
                    {
                        // TokenDto nesnesini oluştur
                        var tokenDto = new TokenDto
                        {
                            AccessToken = session.AccessToken,
                            AccessTokenExpiration = session.AccessTokenExpiration,
                            RefreshToken = session.RefreshToken
                        };

                        // Cookie authentication için sign-in yap
                        await authService.SignInAsync(tokenDto);

                        if (TempData["ReturnUrl"] != null)
                        {
                            var returnUrl = TempData["ReturnUrl"].ToString();
                            if (Url.IsLocalUrl(returnUrl))
                            {
                                return LocalRedirect(returnUrl);
                            }
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Giriş işlemi başarısız oldu.");
                TempData["ReturnUrl"] = ReturnUrl;
                return View(model);

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Bir hata oluştu: {ex.Message}");
                TempData["ReturnUrl"] = ReturnUrl;
                return View(model);
            }
        }


        public async Task<IActionResult> Logout()
        {
            await authService.SignOutAsync();
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> RefreshToken()
        {
            if (await authService.RefreshTokenAsync())
            {
                return RedirectToAction("Index", "Home");
            }


            return RedirectToAction("Login");
        }
        public async Task<IActionResult> AccessDenied()
        {
            #region Rol ve yetkilendirme için problem yaşandığında buradan cookie bilgileri kontrol edilebilir
            // Mevcut cookie ve yetkilendirme bilgilerini kontrol et
            //var identityClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            //var isAuthenticated = User.Identity.IsAuthenticated;
            //var authType = User.Identity.AuthenticationType;
            //var username = User.Identity.Name;
            //var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            //// Bilgileri görüntülemek için ViewBag'e ekle
            //ViewBag.IsAuthenticated = isAuthenticated;
            //ViewBag.AuthType = authType;
            //ViewBag.Username = username;
            //ViewBag.Roles = roles;
            //ViewBag.Claims = identityClaims;
            #endregion

            return View();
        }
    }
}
