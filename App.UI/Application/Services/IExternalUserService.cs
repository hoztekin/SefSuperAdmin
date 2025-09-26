using App.UI.Application.DTOS;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Storage;
using System.Text.Json;

namespace App.UI.Application.Services
{
    public interface IExternalUserService
    {
        Task<ServiceResult<List<ExternalUserListDto>>> GetUsersAsync();
        Task<ServiceResult<ExternalUserDto>> GetUserByIdAsync(string id);
        Task<ServiceResult<ExternalUserDto>> CreateUserAsync(CreateExternalUserDto createDto);
        Task<ServiceResult<ExternalUserDto>> UpdateUserAsync(UpdateExternalUserDto updateDto);
        Task<ServiceResult> DeleteUserAsync(string id);
        Task<ServiceResult> ChangeUserStatusAsync(string id, bool isActive);
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
                    // Desktop pattern: JSON deserialize
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var apiResponse = JsonSerializer.Deserialize<ExternalApiResponse<ExternalUserDto>>(jsonString, options);

                    if (apiResponse?.IsSuccess == true && apiResponse.Data?.List != null)
                    {
                        // DTO mapping
                        var userList = apiResponse.Data.List.Select(u => new ExternalUserListDto
                        {
                            Id = u.Id,
                            UserName = u.Username,
                            Email = u.EMail,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                            IsActive = u.IsActive,
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
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var user = JsonSerializer.Deserialize<ExternalUserDto>(jsonString, options);

                    if (user != null)
                    {
                        return ServiceResult<ExternalUserDto>.Success(user);
                    }
                }

                _logger.LogWarning("Kullanıcı bulunamadı: {UserId}", id);
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı detayı alınırken hata oluştu: {UserId}", id);
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı detayı alınamadı");
            }
        }

        public async Task<ServiceResult<ExternalUserDto>> CreateUserAsync(CreateExternalUserDto createDto)
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

                var externalApiData = new
                {
                    UserName = createDto.UserName,
                    EMail = createDto.Email,
                    Code = createDto.Code, 
                    Password = createDto.Password,
                    ConfirmPassword = createDto.Password, 
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    PhoneNumber = createDto.PhoneNumber,
                    UserLoginType = createDto.LoginType, 
                    IsActive = createDto.IsActive
                };

                var response = await _externalApiService.PostWithTokenAsync(
                    selectedMachine.ApiAddress,
                    "identity/account",
                    externalApiData,
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var createdUser = JsonSerializer.Deserialize<ExternalUserDto>(jsonString, options);

                    if (createdUser != null)
                    {
                        _logger.LogInformation("External kullanıcı başarıyla oluşturuldu: {UserId} - {UserName}",
                            createdUser.Id, createDto.UserName);
                        return ServiceResult<ExternalUserDto>.Success(createdUser);
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("External API Error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı oluşturulamadı - API hatası");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External kullanıcı oluşturulurken hata oluştu: {UserName}", createDto.UserName);
                return ServiceResult<ExternalUserDto>.Fail("External kullanıcı oluşturulurken hata oluştu");
            }
        }

        public async Task<ServiceResult<ExternalUserDto>> UpdateUserAsync(UpdateExternalUserDto updateDto)
        {
            try
            {
                var selectedMachine = _sessionService.GetSelectedMachine();
                if (selectedMachine == null)
                {
                    return ServiceResult<ExternalUserDto>.Fail("Makine seçilmedi");
                }

                // Validation
                if (string.IsNullOrWhiteSpace(updateDto.Id))
                {
                    return ServiceResult<ExternalUserDto>.Fail("Kullanıcı ID'si geçersiz");
                }

                if (string.IsNullOrWhiteSpace(updateDto.UserName))
                {
                    return ServiceResult<ExternalUserDto>.Fail("Kullanıcı adı boş olamaz");
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

                _logger.LogInformation("Kullanıcı güncelleniyor: {UserId}", updateDto.Id);

                var response = await _externalApiService.PutWithTokenAsync(
                    selectedMachine.ApiAddress,
                    $"identity/account",
                    updateDto,
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var updatedUser = JsonSerializer.Deserialize<ExternalUserDto>(jsonString, options);

                    if (updatedUser != null)
                    {
                        _logger.LogInformation("Kullanıcı başarıyla güncellendi: {UserId} - {UserName}",
                            updateDto.Id, updateDto.UserName);

                        return ServiceResult<ExternalUserDto>.Success(updatedUser);
                    }
                }

                _logger.LogWarning("Kullanıcı güncellenemedi: {UserId}", updateDto.Id);
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı güncellenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu: {UserId}", updateDto.Id);
                return ServiceResult<ExternalUserDto>.Fail("Kullanıcı güncellenirken hata oluştu");
            }
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

                _logger.LogInformation("Kullanıcı siliniyor: {UserId}", id);

                var response = await _externalApiService.DeleteWithTokenAsync(
                    selectedMachine.ApiAddress,
                    $"identity/account",
                    token
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Kullanıcı başarıyla silindi: {UserId}", id);
                    return ServiceResult.Success();
                }

                _logger.LogWarning("Kullanıcı silinemedi: {UserId}", id);
                return ServiceResult.Fail("Kullanıcı silinemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu: {UserId}", id);
                return ServiceResult.Fail("Kullanıcı silinirken hata oluştu");
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
