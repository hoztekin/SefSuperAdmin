using App.UI.Application.DTOS;
using System.Text.Json;

namespace App.UI.Infrastructure.Storage
{

    /// <summary>
    /// İki ana sorumluluk: 
    /// 1. Ana uygulama user session yönetimi
    /// 2. Seçili makine ve external API token yönetimi
    /// </summary>
    public interface ISessionService
    {
        // ========== ANA UYGULAMA SESSION YÖNETİMİ ==========
        /// <summary>
        /// Kullanıcı ana uygulamaya login olduktan sonra session'ını kaydeder
        /// </summary>
        void SaveUserSession(string accessToken, string refreshToken, DateTime expiresAt,
                           UserInfoDto userInfo, List<string> roles, List<string> permissions);

        /// <summary>
        /// Ana uygulama için geçerli session bilgilerini döner
        /// </summary>
        UserSessionInfo GetUserSession();

        /// <summary>
        /// Ana uygulama token'ının geçerliliğini kontrol eder
        /// </summary>
        bool IsAuthenticated();

        /// <summary>
        /// Ana uygulama token'ı
        /// </summary>
        string GetUserAccessToken();

        /// <summary>
        /// Kullanıcının belirli bir yetkiye sahip olup olmadığını kontrol eder
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// Kullanıcının belirli bir role sahip olup olmadığını kontrol eder
        /// </summary>
        bool HasRole(string role);

        /// <summary>
        /// Tüm session'ı temizler (logout)
        /// </summary>
        void ClearUserSession();

        // ========== SEÇİLİ MAKİNE VE EXTERNAL API YÖNETİMİ ==========
        /// <summary>
        /// Kullanıcı bir makine seçtikten sonra o makine bilgisini kaydeder
        /// </summary>
        void SaveSelectedMachine(int machineId, string branchId, string branchName, string apiAddress);

        /// <summary>
        /// Seçili makine bilgisini döner
        /// </summary>
        SelectedMachineInfo GetSelectedMachine();

        /// <summary>
        /// Seçili makine var mı kontrol eder
        /// </summary>
        bool HasSelectedMachine();

        /// <summary>
        /// Seçili makine bilgisini temizler
        /// </summary>
        void ClearSelectedMachine();

        /// <summary>
        /// Seçili makinenin API'sine istek atıp alınan token'ı kaydeder
        /// </summary>
        void SaveMachineApiToken(string apiAddress, string accessToken, DateTime expiresAt, string refreshToken = null);

        /// <summary>
        /// Seçili makinenin token'ını döner (varsa ve geçerliyse)
        /// </summary>
        string GetMachineApiToken();

        /// <summary>
        /// Seçili makinenin geçerli token'ı var mı kontrol eder
        /// </summary>
        bool HasValidMachineToken();

        /// <summary>
        /// Seçili makinenin token'ını temizler
        /// </summary>
        void ClearMachineApiToken();

    }

    public class SessionService : ISessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionService> _logger;

        // Ana uygulama session keys
        private const string USER_TOKEN_KEY = "UserToken";
        private const string USER_SESSION_KEY = "UserSession";

        // Seçili makine keys  
        private const string SELECTED_MACHINE_KEY = "SelectedMachine";
        private const string MACHINE_TOKEN_KEY = "MachineToken";

        private ISession Session => _httpContextAccessor.HttpContext?.Session;

        public SessionService(IHttpContextAccessor httpContextAccessor, ILogger<SessionService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        // ========== ANA UYGULAMA SESSION YÖNETİMİ ==========
        public void SaveUserSession(string accessToken, string refreshToken, DateTime expiresAt,
                                   UserInfoDto userInfo, List<string> roles, List<string> permissions)
        {
            try
            {
                var sessionInfo = new UserSessionInfo
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt.ToUniversalTime(),
                    UserInfo = userInfo,
                    Roles = roles ?? new List<string>(),
                    Permissions = permissions ?? new List<string>()
                };

                var json = JsonSerializer.Serialize(sessionInfo);
                Session.SetString(USER_SESSION_KEY, json);

                _logger.LogInformation("User session saved for user: {UserId}", userInfo?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User session kaydedilirken hata");
                throw;
            }
        }

        public UserSessionInfo GetUserSession()
        {
            try
            {
                var json = Session.GetString(USER_SESSION_KEY);
                if (string.IsNullOrEmpty(json)) return null;

                var sessionInfo = JsonSerializer.Deserialize<UserSessionInfo>(json);
                return sessionInfo?.IsExpired == false ? sessionInfo : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User session okunurken hata");
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            var session = GetUserSession();
            return session != null && !session.IsExpired;
        }

        public string GetUserAccessToken()
        {
            var session = GetUserSession();
            return session?.AccessToken;
        }

        public bool HasPermission(string permission)
        {
            if (string.IsNullOrEmpty(permission)) return false;

            var session = GetUserSession();
            return session?.Permissions?.Contains(permission) == true;
        }

        public bool HasRole(string role)
        {
            if (string.IsNullOrEmpty(role)) return false;

            var session = GetUserSession();
            return session?.Roles?.Contains(role) == true;
        }

        public void ClearUserSession()
        {
            try
            {
                Session.Remove(USER_SESSION_KEY);
                Session.Remove(USER_TOKEN_KEY);
                _logger.LogInformation("User session cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User session temizlenirken hata");
            }
        }

        // ========== SEÇİLİ MAKİNE VE EXTERNAL API YÖNETİMİ ==========
        public void SaveSelectedMachine(int machineId, string branchId, string branchName, string apiAddress)
        {
            try
            {
                var machineInfo = new SelectedMachineInfo
                {
                    MachineId = machineId,
                    BranchId = branchId,
                    BranchName = branchName,
                    ApiAddress = apiAddress.TrimEnd('/'),
                    SelectedAt = DateTime.Now
                };

                var json = JsonSerializer.Serialize(machineInfo);
                Session.SetString(SELECTED_MACHINE_KEY, json);

                _logger.LogInformation("Machine selected: {MachineId} - {ApiAddress}", machineId, apiAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Selected machine kaydedilirken hata");
            }
        }

        public SelectedMachineInfo GetSelectedMachine()
        {
            try
            {
                var json = Session.GetString(SELECTED_MACHINE_KEY);
                if (string.IsNullOrEmpty(json)) return null;

                return JsonSerializer.Deserialize<SelectedMachineInfo>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Selected machine okunurken hata");
                return null;
            }
        }

        public bool HasSelectedMachine()
        {
            return GetSelectedMachine() != null;
        }

        public void ClearSelectedMachine()
        {
            try
            {
                Session.Remove(SELECTED_MACHINE_KEY);
                Session.Remove(MACHINE_TOKEN_KEY); // Makine token'ını da temizle
                _logger.LogInformation("Selected machine cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Selected machine temizlenirken hata");
            }
        }

        public void SaveMachineApiToken(string apiAddress, string accessToken, DateTime expiresAt, string refreshToken = null)
        {
            try
            {
                var selectedMachine = GetSelectedMachine();
                if (selectedMachine?.ApiAddress != apiAddress.TrimEnd('/'))
                {
                    _logger.LogWarning("Machine token saved for different API address than selected machine");
                }

                var tokenInfo = new
                {
                    ApiAddress = apiAddress.TrimEnd('/'),
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt.ToUniversalTime()
                };

                var json = JsonSerializer.Serialize(tokenInfo);
                Session.SetString(MACHINE_TOKEN_KEY, json);

                _logger.LogInformation("Machine API token saved for: {ApiAddress}", apiAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Machine API token kaydedilirken hata");
            }
        }

        public string GetMachineApiToken()
        {
            try
            {
                var json = Session.GetString(MACHINE_TOKEN_KEY);
                if (string.IsNullOrEmpty(json)) return null;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var expiresAt = root.GetProperty("ExpiresAt").GetDateTime();
                if (DateTime.UtcNow >= expiresAt)
                {
                    ClearMachineApiToken();
                    return null;
                }

                return root.GetProperty("AccessToken").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Machine API token okunurken hata");
                return null;
            }
        }

        public bool HasValidMachineToken()
        {
            return !string.IsNullOrEmpty(GetMachineApiToken());
        }

        public void ClearMachineApiToken()
        {
            try
            {
                Session.Remove(MACHINE_TOKEN_KEY);
                _logger.LogInformation("Machine API token cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Machine API token temizlenirken hata");
            }
        }

    }

}

