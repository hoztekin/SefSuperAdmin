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
    public class AuthenticationController(IApiService apiService, IAuthService authService, ITokenService tokenService) : Controller
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
                this.SetSuccessMessage("Kayıt başarıyla tamamlandı! Giriş yapabilirsiniz.");
                return RedirectToAction("Login");
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
                // API'den token al
                var tokenResponse = await apiService.PostAsync<TokenDto>("api/v1/Auth/Login", model, false);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    // JWT token'dan kullanıcı bilgilerini çıkar
                    var (userId, roles) = JwtTokenParser.ParseToken(tokenResponse.AccessToken);

                    // Session'ı kaydet
                    SessionManager.SaveSession(
                        tokenResponse.AccessToken,
                        userId,
                        tokenResponse.AccessTokenExpiration,
                        roles,
                        tokenResponse.RefreshToken
                    );

                    // Cookie authentication için sign-in yap
                    await authService.SignInAsync(tokenResponse);

                    this.SetSuccessMessage("Giriş başarılı!");

                    // Return URL kontrolü
                    if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                    {
                        return LocalRedirect(ReturnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Giriş bilgileri hatalı.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Giriş işlemi sırasında hata oluştu: {ex.Message}");
            }

            TempData["ReturnUrl"] = ReturnUrl;
            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await authService.SignOutAsync();
            this.SetInfoMessage("Çıkış işlemi tamamlandı.");
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> RefreshToken()
        {
            if (await authService.RefreshTokenAsync())
            {
                return RedirectToAction("Index", "Home");
            }

            this.SetWarningMessage("Oturum süresi doldu, tekrar giriş yapınız.");
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
            this.SetErrorMessage("Bu sayfaya erişim yetkiniz bulunmamaktadır.");
            return View();
        }
    }
}
