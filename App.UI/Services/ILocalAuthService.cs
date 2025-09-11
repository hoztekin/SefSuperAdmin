using App.Repositories.UserApps;
using App.Services.Authentications.Login;
using App.UI.DTOS;
using Microsoft.AspNetCore.Identity;

namespace App.UI.Services
{
    public interface ILocalAuthService
    {
        Task<AuthResult> LoginAsync(LoginDto model);
        Task<AuthResult> LogoutAsync();
        bool IsAuthenticated();
        UserApp GetCurrentUser();
        List<string> GetCurrentUserRoles();
    }

    public class LocalAuthService : ILocalAuthService
    {
        private readonly UserManager<UserApp> _userManager;
        private readonly SignInManager<UserApp> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LocalAuthService> _logger;

        public LocalAuthService(
            UserManager<UserApp> userManager,
            SignInManager<UserApp> signInManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LocalAuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(LoginDto model)
        {
            try
            {
                _logger.LogInformation("Local login işlemi başlatılıyor: {UserName}", model.UserName);

                // Kullanıcıyı bul
                var user = await _userManager.FindByNameAsync(model.UserName) ??
                          await _userManager.FindByEmailAsync(model.UserName);

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı: {UserName}", model.UserName);
                    return AuthResult.Failed("Kullanıcı adı veya şifre hatalı");
                }

                // Şifre kontrolü
                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    _logger.LogInformation("Kullanıcı başarıyla giriş yaptı: {UserName}, Roles: {Roles}",
                        model.UserName, string.Join(", ", roles));

                    return AuthResult.Success($"Hoş geldiniz, {user.UserName}")
                        .WithData("UserId", user.Id)
                        .WithData("Roles", roles.ToList());
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Kullanıcı hesabı kilitli: {UserName}", model.UserName);
                    return AuthResult.Failed("Hesabınız geçici olarak kilitlenmiştir. Lütfen daha sonra tekrar deneyin.");
                }

                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("İki faktörlü doğrulama gerekli: {UserName}", model.UserName);
                    return AuthResult.Failed("İki faktörlü doğrulama gerekli");
                }

                _logger.LogWarning("Login başarısız: {UserName}", model.UserName);
                return AuthResult.Failed("Kullanıcı adı veya şifre hatalı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login işleminde beklenmeyen hata: {UserName}", model.UserName);
                return AuthResult.Failed("Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.");
            }
        }

        public async Task<AuthResult> LogoutAsync()
        {
            try
            {
                var user = GetCurrentUser();

                await _signInManager.SignOutAsync();

                _logger.LogInformation("Kullanıcı çıkış yaptı: {UserId}", user?.Id);

                return AuthResult.Success("Başarıyla çıkış yapıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout işleminde hata oluştu");
                return AuthResult.Failed("Çıkış işleminde hata oluştu");
            }
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }

        public UserApp GetCurrentUser()
        {
            if (!IsAuthenticated())
                return null;

            var userId = _httpContextAccessor.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return null;

            return _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
        }

        public List<string> GetCurrentUserRoles()
        {
            var user = GetCurrentUser();

            if (user == null)
                return new List<string>();

            return _userManager.GetRolesAsync(user).GetAwaiter().GetResult().ToList();
        }
    }
}
