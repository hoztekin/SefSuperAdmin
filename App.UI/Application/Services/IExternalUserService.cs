using App.UI.Application.DTOS;
using App.UI.Application.Enums;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Storage;
using System.Text.Json;

namespace App.UI.Application.Services
{
    public interface IExternalUserService
    {
        Task<ServiceResult<List<ExternalUserListDto>>>  GetUsersAsync();
        Task<ServiceResult<ExternalUserDto>> GetUserByIdAsync(string id);
        Task<ServiceResult<ExternalUserDto>> CreateUserAsync(CreateExternalUserDto createDto);
        Task<ServiceResult<ExternalUserDto>> UpdateUserAsync(UpdateExternalUserDto updateDto);
        Task<ServiceResult> DeleteUserAsync(string id);
        Task<ServiceResult> ChangeUserStatusAsync(string id, bool isActive);
        Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto changePasswordDto); 
    }

    public class ExternalUserService : IExternalUserService
    {
        private readonly IExternalApiService _externalApiService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<ExternalUserService> _logger;

        public ExternalUserService(
            IExternalApiService externalApiService,
            ISessionService sessionService,
            ILogger<ExternalUserService> logger)
        {
            _externalApiService = externalApiService;
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task<ServiceResult<List<ExternalUserListDto>>> GetUsersAsync()
        {
            try
            {
                var selectedMachine = _sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return ServiceResult<List<ExternalUserListDto>>.Fail("Makine seçilmedi");
                }

                var token = _sessionService.GetMachineApiToken();
                if (string.IsNullOrEmpty(token))
                {
                    var loginResponse = await _externalApiService.LoginAsync(selectedMachine.ApiAddress);
                    if (!loginResponse.Success)
                    {
                        return ServiceResult<List<ExternalUserListDto>>.Fail("Token alınamadı");
                    }
                    token = loginResponse.AccessToken;
                }

                // Desktop pattern: HttpResponseMessage al
                var response = await _externalApiService.GetWithTokenAsync(
                    selectedMachine.ApiAddress,
                    "identity/account",
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var apiResponse = JsonSerializer.Deserialize<ExternalApiResponse<ExternalUserDto>>(jsonString, options);

                    if (apiResponse?.IsSuccess == true && apiResponse.Data?.List != null)
                    {
                        // DTO mapping
                        var userList = apiResponse.Data.List.Where(x => x.IsActive).Select(u => new ExternalUserListDto
                        {
                            Id = u.Id,
                            UserName = u.Username,
                            Email = u.EMail,
                            Code = u.Code,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                            CompanyName = u.CompanyName,
                            BranchName = u.BranchName
                        }).ToList();

                        return ServiceResult<List<ExternalUserListDto>>.Success(userList);
                    }
                }

                return ServiceResult<List<ExternalUserListDto>>.Fail("Kullanıcılar yüklenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API'den kullanıcılar alınırken hata oluştu");
                return ServiceResult<List<ExternalUserListDto>>.Fail("Kullanıcılar yüklenirken hata oluştu");
            }
        }

        public async Task<ServiceResult<ExternalUserDto>> GetUserByIdAsync(string id)
        {
            try
            {
                var selectedMachine = _sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return ServiceResult<ExternalUserDto>.Fail("Makine seçilmedi");
                }

                var token = _sessionService.GetMachineApiToken();
                if (string.IsNullOrEmpty(token))
                {
                    var loginResponse = await _externalApiService.LoginAsync(selectedMachine.ApiAddress);
                    if (!loginResponse.Success)
                    {
                        return ServiceResult<ExternalUserDto>.Fail("Token alınamadı");
                    }
                    token = loginResponse.AccessToken;
                }

                _logger.LogInformation("External API'den kullanıcı detayı alınıyor: {UserId}", id);

                var response = await _externalApiService.GetWithTokenAsync(
                    selectedMachine.ApiAddress,
                    $"identity/account/{id}",
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("API Response: {Json}", jsonString.Length > 500 ? jsonString.Substring(0, 500) : jsonString);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, options);
        
                    if (apiResponse.TryGetProperty("data", out var dataElement))
                    {
                        JsonElement userElement;

                        if (dataElement.TryGetProperty("item", out var itemElement))
                        {
                            userElement = itemElement;
                        }
                        else
                        {
                            userElement = dataElement;
                        }

                        var user = new ExternalUserDto
                        {
                            Id = GetStringProperty(userElement, "id"),
                            Username = GetStringProperty(userElement, "username") ?? GetStringProperty(userElement, "userName"),
                            EMail = GetStringProperty(userElement, "eMail") ?? GetStringProperty(userElement, "email"),
                            FirstName = GetStringProperty(userElement, "firstName"),
                            LastName = GetStringProperty(userElement, "lastName"),
                            PhoneNumber = GetStringProperty(userElement, "phoneNumber"),
                            Code = GetStringProperty(userElement, "code"),
                            BranchName = GetStringProperty(userElement, "branchName"),
                            CompanyName = GetStringProperty(userElement, "companyName"),
                            EmailConfirmed = GetBoolProperty(userElement, "emailConfirmed")
                        };

                        var loginTypeStr = GetStringProperty(userElement, "userLoginType");
                        if (!string.IsNullOrEmpty(loginTypeStr))
                        {
                            if (Enum.TryParse<UserLoginType>(loginTypeStr, true, out var loginType))
                            {
                                user.UserLoginType = loginType;
                            }
                        }
                        else
                        {
                            if (userElement.TryGetProperty("userLoginType", out var loginTypeElement) &&
                                loginTypeElement.TryGetInt32(out var loginTypeInt))
                            {
                                user.UserLoginType = (UserLoginType)loginTypeInt;
                            }
                        }

                        if (userElement.TryGetProperty("roles", out var rolesElement) &&
                            rolesElement.ValueKind == JsonValueKind.Array)
                        {
                            user.Roles = new List<string>();
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleStr = role.GetString();
                                if (!string.IsNullOrEmpty(roleStr))
                                {
                                    user.Roles.Add(roleStr);
                                }
                            }
                        }

                        _logger.LogInformation("Kullanıcı başarıyla parse edildi: {UserId} - UserName: {UserName}, Email: {Email}",
                            user.Id, user.Username, user.EMail);

                        return ServiceResult<ExternalUserDto>.Success(user);
                    }
                    else
                    {
                        _logger.LogWarning("API response'unda 'data' property'si bulunamadı");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("API hatası: {StatusCode} - {Content}", response.StatusCode, errorContent);
                }

                _logger.LogWarning("Kullanıcı bulunamadı: {UserId}", id);
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı detayı alınırken hata oluştu: {UserId}", id);
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı detayı alınamadı: " + ex.Message);
            }
        }

        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind != JsonValueKind.Null)
            {
                return prop.GetString();
            }
            return null;
        }

        private bool GetBoolProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.True)
            {
                return true;
            }
            if (prop.ValueKind == JsonValueKind.False)
            {
                return false;
            }
            return false;
        }
        // ✅ Helper: Token al veya login yap
        private async Task<ServiceResult<string>> GetTokenAsync()
        {
            var selectedMachine = _sessionService.GetSelectedMachine();
            if (selectedMachine == null)
            {
                return ServiceResult<string>.Fail("Makine seçilmedi");
            }

            var token = _sessionService.GetMachineApiToken();
            if (!string.IsNullOrEmpty(token))
            {
                return ServiceResult<string>.Success(token);
            }

            var loginResponse = await _externalApiService.LoginAsync(selectedMachine.ApiAddress);
            if (!loginResponse.Success)
            {
                return ServiceResult<string>.Fail("Token alınamadı");
            }

            return ServiceResult<string>.Success(loginResponse.AccessToken);
        }

        // ✅ Helper: Selected Machine al
        private SelectedMachineInfo? GetSelectedMachine()
        {
            var machine = _sessionService.GetSelectedMachine();
            if (machine == null)
            {
                _logger.LogWarning("Makine seçilmedi");
            }
            return machine;
        }

        // ✅ Helper: JSON parse et ve hata mesajı al
        private (bool isSuccess, string message, ExternalUserDto? user) ParseCreateUpdateResponse(
            string jsonString, HttpResponseMessage response)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    var root = doc.RootElement;

                    bool isSuccess = false;
                    string message = "Bilinmeyen hata oluştu";

                    if (root.TryGetProperty("isSuccess", out var isSuccessElement))
                    {
                        isSuccess = isSuccessElement.GetBoolean();
                    }

                    if (root.TryGetProperty("message", out var messageElement))
                    {
                        message = messageElement.GetString() ?? message;
                    }

                    // Hata varsa, sadece mesajı dön
                    if (!isSuccess || !response.IsSuccessStatusCode)
                    {
                        return (false, message, null);
                    }

                    // Başarısı durumda user'ı deserialize et
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var user = JsonSerializer.Deserialize<ExternalUserDto>(jsonString, options);

                    return (isSuccess && response.IsSuccessStatusCode, message, user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON parse hatası");
                return (false, $"Hata: {ex.Message}", null);
            }
        }

        // ✅ CREATE - Tüm business logic Service'de
        public async Task<ServiceResult<ExternalUserDto>> CreateUserAsync(CreateExternalUserDto createDto)
        {
            try
            {
                var machine = GetSelectedMachine();
                if (machine == null)
                    return ServiceResult<ExternalUserDto>.Fail("Makine seçilmedi");

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<ExternalUserDto>.Fail(tokenResult.Message);

                var externalApiData = new
                {
                    UserName = createDto.UserName,
                    Email = createDto.Email,
                    Code = createDto.Code,
                    Password = createDto.Password,
                    ConfirmPassword = createDto.Password,
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    PhoneNumber = createDto.PhoneNumber,
                    UserLoginType = createDto.LoginType
                };

                var response = await _externalApiService.PostWithTokenAsync(
                    machine.ApiAddress,
                    "identity/account",
                    externalApiData,
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var (isSuccess, message, user) = ParseCreateUpdateResponse(jsonString, response);

                if (!isSuccess)
                {
                    _logger.LogWarning("Kullanıcı oluşturulamadı: {Message}", message);
                    return ServiceResult<ExternalUserDto>.Fail(message);
                }

                if (user != null)
                {
                    _logger.LogInformation("Kullanıcı oluşturuldu: {UserId} - {UserName}", user.Id, createDto.UserName);
                    return ServiceResult<ExternalUserDto>.Success(user);
                }

                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı verisi alınamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı oluşturulurken hata: {UserName}", createDto.UserName);
                return ServiceResult<ExternalUserDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ UPDATE - Tüm business logic Service'de
        public async Task<ServiceResult<ExternalUserDto>> UpdateUserAsync(UpdateExternalUserDto updateDto)
        {
            try
            {
                // ✅ Validation Service'de
                var validationError = ValidateUpdateUser(updateDto);
                if (!validationError.IsSuccess)
                    return validationError;

                var machine = GetSelectedMachine();
                if (machine == null)
                    return ServiceResult<ExternalUserDto>.Fail("Makine seçilmedi");

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<ExternalUserDto>.Fail(tokenResult.Message);

                var requestObject = new
                {
                    id = updateDto.Id,
                    email = updateDto.Email,
                    isActive = true,
                    phoneNumber = updateDto.PhoneNumber,
                    firstName = updateDto.FirstName,
                    lastName = updateDto.LastName,
                    code = updateDto.Code,
                    roles = updateDto.Roles ?? new List<string>(),
                    userLoginType = updateDto.UserLoginType.ToString(),
                    password = string.IsNullOrEmpty(updateDto.Password) ? null : updateDto.Password
                };

                var response = await _externalApiService.PutWithTokenAsync(
                    machine.ApiAddress,
                    "identity/account",
                    requestObject,
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var (isSuccess, message, user) = ParseCreateUpdateResponse(jsonString, response);

                if (!isSuccess)
                {
                    _logger.LogWarning("Kullanıcı güncellenemedi: {Id} - {Message}", updateDto.Id, message);
                    return ServiceResult<ExternalUserDto>.Fail(message);
                }

                if (user != null)
                {
                    _logger.LogInformation("Kullanıcı güncellendi: {UserId}", updateDto.Id);
                    return ServiceResult<ExternalUserDto>.Success(user);
                }

                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı verisi alınamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata: {UserId}", updateDto.Id);
                return ServiceResult<ExternalUserDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ CHANGE PASSWORD - Tüm business logic Service'de
        public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
        {
            try
            {
                // Validation Service'de
                if (string.IsNullOrEmpty(changePasswordDto.Id))
                    return ServiceResult.Fail("Geçersiz kullanıcı ID'si");

                if (string.IsNullOrEmpty(changePasswordDto.NewPassword) || changePasswordDto.NewPassword.Length < 6)
                    return ServiceResult.Fail("Parola en az 6 karakter olmalıdır");

                if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
                    return ServiceResult.Fail("Parolalar eşleşmiyor");

                var machine = GetSelectedMachine();
                if (machine == null)
                    return ServiceResult.Fail("Makine seçilmedi");

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult.Fail(tokenResult.Message);

                var updateData = new
                {
                    Id = changePasswordDto.Id,
                    NewPassword = changePasswordDto.NewPassword,
                    ConfirmNewPassword = changePasswordDto.ConfirmNewPassword
                };

                var response = await _externalApiService.PutWithTokenAsync(
                    machine.ApiAddress,
                    "identity/account/update-password",
                    updateData,
                    tokenResult.Data
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Parola değiştirilirken hata: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    return ServiceResult.Fail("Parola değiştirilirken hata oluştu");
                }

                _logger.LogInformation("Parola değiştirildi: {UserId}", changePasswordDto.Id);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parola değiştirilirken hata: {UserId}", changePasswordDto.Id);
                return ServiceResult.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ Validation Helper
        private ServiceResult<ExternalUserDto> ValidateUpdateUser(UpdateExternalUserDto updateDto)
        {
            if (string.IsNullOrWhiteSpace(updateDto.Id))
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı ID'si geçersiz");

            if (string.IsNullOrWhiteSpace(updateDto.Email))
                return ServiceResult<ExternalUserDto>.Fail("Email adresi zorunludur");

            if (string.IsNullOrWhiteSpace(updateDto.FirstName) || updateDto.FirstName.Length < 2)
                return ServiceResult<ExternalUserDto>.Fail("Ad en az 2 karakter olmalıdır");

            if (string.IsNullOrWhiteSpace(updateDto.LastName) || updateDto.LastName.Length < 2)
                return ServiceResult<ExternalUserDto>.Fail("Soyad en az 2 karakter olmalıdır");

            if (string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
                return ServiceResult<ExternalUserDto>.Fail("Telefon numarası zorunludur");

            if (!System.Text.RegularExpressions.Regex.IsMatch(updateDto.PhoneNumber, @"^0\d{10}$"))
                return ServiceResult<ExternalUserDto>.Fail("Telefon numarası 0 ile başlamalı ve 11 haneli olmalıdır");

            if (string.IsNullOrWhiteSpace(updateDto.Code) || updateDto.Code.Length < 2 || updateDto.Code.Length > 20)
                return ServiceResult<ExternalUserDto>.Fail("Kod 2-20 karakter arasında olmalıdır");

            if (!System.Text.RegularExpressions.Regex.IsMatch(updateDto.Code, @"^[A-Z0-9]+$"))
                return ServiceResult<ExternalUserDto>.Fail("Kod sadece büyük harf ve rakam içermelidir");

            if (!string.IsNullOrEmpty(updateDto.Password) && updateDto.Password.Length < 6)
                return ServiceResult<ExternalUserDto>.Fail("Şifre en az 6 karakter olmalıdır");

            return ServiceResult<ExternalUserDto>.Success(null);
        }

        public async Task<ServiceResult> DeleteUserAsync(string id)
        {
            try
            {
                var selectedMachine = _sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return ServiceResult.Fail("Makine seçilmedi");
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    return ServiceResult.Fail("Kullanıcı ID'si geçersiz");
                }

                var token = _sessionService.GetMachineApiToken();
                if (string.IsNullOrEmpty(token))
                {
                    var loginResponse = await _externalApiService.LoginAsync(selectedMachine.ApiAddress);
                    if (!loginResponse.Success)
                    {
                        return ServiceResult.Fail("Token alınamadı");
                    }
                    token = loginResponse.AccessToken;
                }

                _logger.LogInformation("External kullanıcı siliniyor: {UserId}", id);

                // DELETE body için DTO hazırla
                var deleteDto = new DeleteDto
                {
                    Id = id,

                };

                var response = await _externalApiService.DeleteWithTokenAsync(
                    selectedMachine.ApiAddress,
                    "identity/account",
                    deleteDto,
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("External kullanıcı başarıyla silindi: {UserId}", id);
                    return ServiceResult.Success();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Kullanıcı silinemedi: {UserId}, StatusCode: {StatusCode}, Error: {Error}",
                        id, response.StatusCode, errorContent);

                    return ServiceResult.Fail($"Kullanıcı silinemedi: HTTP {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu: {UserId}", id);
                return ServiceResult.Fail("Kullanıcı silinirken hata oluştu: " + ex.Message);
            }
        }

        public async Task<ServiceResult> ChangeUserStatusAsync(string id, bool isActive)
        {
            try
            {
                var selectedMachine = _sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return ServiceResult.Fail("Makine seçilmedi");
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    return ServiceResult.Fail("Kullanıcı ID'si geçersiz");
                }

                var token = _sessionService.GetMachineApiToken();
                if (string.IsNullOrEmpty(token))
                {
                    var loginResponse = await _externalApiService.LoginAsync(selectedMachine.ApiAddress);
                    if (!loginResponse.Success)
                    {
                        return ServiceResult.Fail("Token alınamadı");
                    }
                    token = loginResponse.AccessToken;
                }

                _logger.LogInformation("Kullanıcı durumu değiştiriliyor: {UserId} - {IsActive}", id, isActive);

                var updateData = new { IsActive = isActive };
                var response = await _externalApiService.PutWithTokenAsync(
                    selectedMachine.ApiAddress,
                    $"identity/account/{id}/status",
                    updateData,
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Kullanıcı durumu başarıyla değiştirildi: {UserId} - {IsActive}", id, isActive);
                    return ServiceResult.Success();
                }

                _logger.LogWarning("Kullanıcı durumu değiştirilemedi: {UserId}", id);
                return ServiceResult.Fail("Kullanıcı durumu değiştirilemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı durumu değiştirilirken hata oluştu: {UserId}", id);
                return ServiceResult.Fail("Kullanıcı durumu değiştirilirken hata oluştu");
            }
        }
    }
}
