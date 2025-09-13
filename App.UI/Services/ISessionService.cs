using App.UI.DTOS;
using System.Text.Json;
using static App.UI.Helper.SessionManager;

namespace App.UI.Services
{
    public interface ISessionService
    {
        void SaveAuthSession(AuthResponseDto authResponse);
        void SaveLoginUsername(string userName);
        SessionInfo GetCurrentSession();
        bool IsAuthenticated();
        bool HasPermission(string permission);
        bool HasAnyPermission(params string[] permissions);
        bool HasRole(string role);
        List<string> GetUserPermissions();
        UserInfoDto GetCurrentUser();
        void ClearSession();
        bool IsTokenExpiring(int minutesThreshold = 5);
        string GetAccessToken();
        string GetRefreshToken();
        ISession GetSession();
        SelectedMachineInfo GetSelectedMachine();
        void ClearSelectedMachine();
        bool HasSelectedMachine();
        void SaveSelectedMachine(int machineId, string branchId, string branchName, string apiAddress);

    }

    public class SessionService : ISessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionService> _logger;
        private const string SESSION_SELECTED_MACHINE_KEY = "SelectedMachine";
        private const string SELECTED_MACHINE_KEY = "SelectedMachine";
        // Session Keys
        private const string SESSION_TOKEN_KEY = "AuthToken";
        private const string SESSION_USER_KEY = "UserInfo";
        private const string SESSION_STAFFS_KEY = "AuthorizedStaffs";
        private const string SESSION_ROLES_KEY = "UserRoles";
        private const string SESSION_PERMISSIONS_KEY = "UserPermissions";
        private const string SESSION_TOKEN_EXPIRES_KEY = "TokenExpiresAt";
        private const string SESSION_REFRESH_TOKEN_KEY = "RefreshToken";
        private const string SESSION_USERNAME_KEY = "Username";

        public SessionService(IHttpContextAccessor httpContextAccessor, ILogger<SessionService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session;

        public void SaveSelectedMachine(int machineId, string branchId, string branchName, string apiAddress)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                var machineInfo = new SelectedMachineInfo
                {
                    MachineId = machineId,
                    BranchId = branchId,
                    BranchName = branchName,
                    ApiAddress = apiAddress,
                    SelectedAt = DateTime.Now
                };

                var json = System.Text.Json.JsonSerializer.Serialize(machineInfo);
                session.SetString(SELECTED_MACHINE_KEY, json);
            }
        }


        public SelectedMachineInfo? GetSelectedMachine()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                var json = session.GetString(SELECTED_MACHINE_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<SelectedMachineInfo>(json);
                }
            }
            return null;
        }

        public void ClearSelectedMachine()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Remove(SELECTED_MACHINE_KEY);
        }

        public bool HasSelectedMachine()
        {
            var selectedMachine = GetSelectedMachine();
            return selectedMachine != null;
        }

        public void SaveAuthSession(AuthResponseDto authResponse)
        {
            try
            {
                if (authResponse?.Data?.Token == null)
                {
                    _logger.LogError("Auth response veya token bilgisi null");
                    return;
                }

                var token = authResponse.Data.Token;
                var userInfo = authResponse.Data.UserInfo;
                var roles = authResponse.Data.Roles ?? new List<string>();

                // Token bilgilerini kaydet
                Session.SetString(SESSION_TOKEN_KEY, token.AccessToken);
                Session.SetString(SESSION_REFRESH_TOKEN_KEY, token.RefreshToken ?? "");

                // Token expiration'ı hesapla (şu an + expires_in saniye)
                var expiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
                Session.SetString(SESSION_TOKEN_EXPIRES_KEY, expiresAt.ToString("O"));

                // User bilgilerini kaydet
                if (userInfo != null)
                {
                    Session.SetString(SESSION_USER_KEY, JsonSerializer.Serialize(userInfo));
                    // Username'i ayrı olarak kaydet (refresh token için gerekli)
                    Session.SetString(SESSION_USERNAME_KEY, userInfo.Email);
                }


                // Rolleri kaydet
                Session.SetString(SESSION_ROLES_KEY, JsonSerializer.Serialize(roles));

                // Tüm permissions'ları topla (user + authorized staffs permissions)
                var allPermissions = new List<string>();

                // User'ın kendi permissions'larını JWT'den çıkarabilirsin


                // Duplicate'leri temizle
                var uniquePermissions = allPermissions.Distinct().ToList();
                Session.SetString(SESSION_PERMISSIONS_KEY, JsonSerializer.Serialize(uniquePermissions));

                _logger.LogInformation("Auth session başarıyla kaydedildi. User: {UserId}, Roles: {RoleCount}, Permissions: {PermissionCount}",
                    userInfo?.Id, roles.Count, uniquePermissions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auth session kaydedilirken hata oluştu");
                throw;
            }
        }

        public SessionInfo GetCurrentSession()
        {
            try
            {
                var token = Session.GetString(SESSION_TOKEN_KEY);
                if (string.IsNullOrEmpty(token))
                    return null;

                var expiresAtStr = Session.GetString(SESSION_TOKEN_EXPIRES_KEY);
                var refreshToken = Session.GetString(SESSION_REFRESH_TOKEN_KEY);

                if (!DateTime.TryParse(expiresAtStr, out var expiresAt))
                    return null;

                var userJson = Session.GetString(SESSION_USER_KEY);
                var rolesJson = Session.GetString(SESSION_ROLES_KEY);
                var permissionsJson = Session.GetString(SESSION_PERMISSIONS_KEY);

                var userInfo = !string.IsNullOrEmpty(userJson)
                    ? JsonSerializer.Deserialize<UserInfoDto>(userJson)
                    : null;

                var roles = !string.IsNullOrEmpty(rolesJson)
                    ? JsonSerializer.Deserialize<List<string>>(rolesJson)
                    : new List<string>();

                var permissions = !string.IsNullOrEmpty(permissionsJson)
                    ? JsonSerializer.Deserialize<List<string>>(permissionsJson)
                    : new List<string>();

                return new SessionInfo
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    UserId = userInfo?.Id,
                    AccessTokenExpiration = expiresAt,
                    UserInfo = userInfo,
                    Roles = roles,
                    Permissions = permissions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session bilgileri okunurken hata oluştu");
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            var session = GetCurrentSession();
            return session != null && session.AccessTokenExpiration > DateTime.UtcNow;
        }

        public bool HasPermission(string permission)
        {
            if (string.IsNullOrEmpty(permission))
                return false;

            var permissions = GetUserPermissions();
            return permissions.Contains(permission);
        }

        public bool HasAnyPermission(params string[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                return false;

            var userPermissions = GetUserPermissions();
            return permissions.Any(p => userPermissions.Contains(p));
        }

        public bool HasRole(string role)
        {
            if (string.IsNullOrEmpty(role))
                return false;

            var session = GetCurrentSession();
            return session?.Roles?.Contains(role) == true;
        }

        public List<string> GetUserPermissions()
        {
            var session = GetCurrentSession();
            return session?.Permissions ?? new List<string>();
        }



        public UserInfoDto GetCurrentUser()
        {
            var session = GetCurrentSession();
            return session?.UserInfo;
        }

        public void ClearSession()
        {
            try
            {
                Session.Remove(SESSION_TOKEN_KEY);
                Session.Remove(SESSION_REFRESH_TOKEN_KEY);
                Session.Remove(SESSION_TOKEN_EXPIRES_KEY);
                Session.Remove(SESSION_USER_KEY);
                Session.Remove(SESSION_ROLES_KEY);
                Session.Remove(SESSION_PERMISSIONS_KEY);
                Session.Remove(SESSION_USERNAME_KEY);
                Session.Remove(SESSION_SELECTED_MACHINE_KEY);

                _logger.LogInformation("Session temizlendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session temizlenirken hata oluştu");
            }
        }

        public bool IsTokenExpiring(int minutesThreshold = 5)
        {
            var session = GetCurrentSession();
            if (session == null)
                return true;

            var remainingTime = (session.AccessTokenExpiration - DateTime.UtcNow).TotalMinutes;
            return remainingTime <= minutesThreshold;
        }

        public string GetAccessToken()
        {
            return Session.GetString(SESSION_TOKEN_KEY);
        }

        public string GetRefreshToken()
        {
            return Session.GetString(SESSION_REFRESH_TOKEN_KEY);
        }
        public string GetUsername()
        {
            return Session.GetString(SESSION_USERNAME_KEY);
        }

        public void SaveLoginUsername(string username)
        {
            Session.SetString(SESSION_USERNAME_KEY, username);
        }

        public ISession GetSession()
        {
            return Session;
        }
    }
}
