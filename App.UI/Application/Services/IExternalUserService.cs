using App.UI.Application.DTOS;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Storage;
using System.Text.Json;

namespace App.UI.Application.Services
{
    public interface IExternalUserService
    {
        Task<ServiceResult<List<ExternalUserListDto>>> GetUsersAsync();
        //Task<ServiceResult<ExternalUserDto>> GetUserByIdAsync(int id);
        //Task<ServiceResult<ExternalUserDto>> CreateUserAsync(CreateExternalUserDto createDto);
        //Task<ServiceResult<ExternalUserDto>> UpdateUserAsync(UpdateExternalUserDto updateDto);
        //Task<ServiceResult> DeleteUserAsync(int id);
        //Task<ServiceResult<ExternalUserStatsDto>> GetUserStatsAsync();
        //Task<ServiceResult> ChangeUserStatusAsync(int id, bool isActive);
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
                            CreatedDate = Convert.ToDateTime(u.CreatedDate)
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

        //public async Task<ServiceResult<ExternalUserDto>> GetUserByIdAsync(int id)
        //{
        //    try
        //    {
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Makine seçilmedi");
        //        }

        //        _logger.LogInformation("External API'den kullanıcı detayı alınıyor: {UserId}", id);

        //        var user = await _externalApiService.GetAsync<ExternalUserDto>(
        //            selectedMachine.ApiAddress,
        //            $"api/v1/users/{id}"
        //        );

        //        if (user == null)
        //        {
        //            _logger.LogWarning("Kullanıcı bulunamadı: {UserId}", id);
        //            return ServiceResult<ExternalUserDto>.Fail("Kullanıcı bulunamadı");
        //        }

        //        return ServiceResult<ExternalUserDto>.Success(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Kullanıcı detayı alınırken hata oluştu: {UserId}", id);
        //        return ServiceResult<ExternalUserDto>.Fail("Kullanıcı detayı alınamadı");
        //    }
        //}

        //public async Task<ServiceResult<ExternalUserDto>> CreateUserAsync(CreateExternalUserDto createDto)
        //{
        //    try
        //    {
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Makine seçilmedi");
        //        }

        //        // Validation
        //        if (string.IsNullOrWhiteSpace(createDto.UserName))
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Kullanıcı adı boş olamaz");
        //        }

        //        if (string.IsNullOrWhiteSpace(createDto.FirstName))
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Ad alanı boş olamaz");
        //        }

        //        if (string.IsNullOrWhiteSpace(createDto.LastName))
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Soyad alanı boş olamaz");
        //        }

        //        if (string.IsNullOrWhiteSpace(createDto.Password))
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Şifre boş olamaz");
        //        }

        //        _logger.LogInformation("Yeni kullanıcı oluşturuluyor: {UserName}", createDto.UserName);

        //        var createdUser = await _externalApiService.PostAsync<ExternalUserDto>(
        //            selectedMachine.ApiAddress,
        //            "api/v1/users",
        //            createDto
        //        );

        //        if (createdUser == null)
        //        {
        //            _logger.LogWarning("Kullanıcı oluşturulamadı: {UserName}", createDto.UserName);
        //            return ServiceResult<ExternalUserDto>.Fail("Kullanıcı oluşturulamadı");
        //        }

        //        _logger.LogInformation("Kullanıcı başarıyla oluşturuldu: {UserId} - {UserName}",
        //            createdUser.Id, createdUser.UserName);

        //        return ServiceResult<ExternalUserDto>.Success(createdUser);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Kullanıcı oluşturulurken hata oluştu: {UserName}", createDto.UserName);
        //        return ServiceResult<ExternalUserDto>.Fail("Kullanıcı oluşturulurken hata oluştu");
        //    }
        //}

        //public async Task<ServiceResult<ExternalUserDto>> UpdateUserAsync(UpdateExternalUserDto updateDto)
        //{
        //    try
        //    {
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Makine seçilmedi");
        //        }

        //        // Validation
        //        if (string.IsNullOrWhiteSpace(updateDto.UserName))
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Kullanıcı adı boş olamaz");
        //        }

        //        if (string.IsNullOrWhiteSpace(updateDto.FirstName))
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Ad alanı boş olamaz");
        //        }

        //        if (string.IsNullOrWhiteSpace(updateDto.LastName))
        //        {
        //            return ServiceResult<ExternalUserDto>.Fail("Soyad alanı boş olamaz");
        //        }

        //        _logger.LogInformation("Kullanıcı güncelleniyor: {UserId} - {UserName}",
        //            updateDto.Id, updateDto.UserName);

        //        var updatedUser = await _externalApiService.PostAsync<ExternalUserDto>(
        //            selectedMachine.ApiAddress,
        //            $"api/v1/users/{updateDto.Id}",
        //            updateDto
        //        );

        //        if (updatedUser == null)
        //        {
        //            _logger.LogWarning("Kullanıcı güncellenemedi: {UserId}", updateDto.Id);
        //            return ServiceResult<ExternalUserDto>.Fail("Kullanıcı güncellenemedi");
        //        }

        //        _logger.LogInformation("Kullanıcı başarıyla güncellendi: {UserId}", updatedUser.Id);
        //        return ServiceResult<ExternalUserDto>.Success(updatedUser);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu: {UserId}", updateDto.Id);
        //        return ServiceResult<ExternalUserDto>.Fail("Kullanıcı güncellenirken hata oluştu");
        //    }
        //}

        //public async Task<ServiceResult> DeleteUserAsync(int id)
        //{
        //    try
        //    {
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return ServiceResult.Fail("Makine seçilmedi");
        //        }

        //        _logger.LogInformation("Kullanıcı siliniyor: {UserId}", id);

        //        // External API'ye DELETE isteği gönder
        //        var result = await _externalApiService.PostAsync<object>(
        //            selectedMachine.ApiAddress,
        //            $"api/v1/users/{id}/delete",
        //            new { }
        //        );

        //        // Silme işlemi genellikle 200 OK döndürür, null result normal olabilir
        //        _logger.LogInformation("Kullanıcı başarıyla silindi: {UserId}", id);
        //        return ServiceResult.Success();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Kullanıcı silinirken hata oluştu: {UserId}", id);
        //        return ServiceResult.Fail("Kullanıcı silinirken hata oluştu");
        //    }
        //}

        //public async Task<ServiceResult<ExternalUserStatsDto>> GetUserStatsAsync()
        //{
        //    try
        //    {
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return ServiceResult<ExternalUserStatsDto>.Fail("Makine seçilmedi");
        //        }

        //        _logger.LogInformation("External API'den kullanıcı istatistikleri alınıyor");

        //        // Önce tüm kullanıcıları al, sonra istatistikleri hesapla
        //        var usersResult = await GetUsersAsync();

        //        if (!usersResult.IsSuccess)
        //        {
        //            return ServiceResult<ExternalUserStatsDto>.Fail("İstatistikler hesaplanamadı");
        //        }

        //        var users = usersResult.Data ?? new List<ExternalUserListDto>();

        //        var stats = new ExternalUserStatsDto
        //        {
        //            TotalUsers = users.Count,
        //            ActiveUsers = users.Count(u => u.IsActive),
        //            InactiveUsers = users.Count(u => !u.IsActive),
        //            RecentUsers = users.Count(u => u.CreatedDate >= DateTime.Now.AddDays(-30)),
        //            LastUpdate = DateTime.Now
        //        };

        //        _logger.LogInformation("Kullanıcı istatistikleri hesaplandı: {Total} toplam, {Active} aktif",
        //            stats.TotalUsers, stats.ActiveUsers);

        //        return ServiceResult<ExternalUserStatsDto>.Success(stats);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Kullanıcı istatistikleri alınırken hata oluştu");
        //        return ServiceResult<ExternalUserStatsDto>.Fail("İstatistikler yüklenirken hata oluştu");
        //    }
        //}

        //public async Task<ServiceResult> ChangeUserStatusAsync(int id, bool isActive)
        //{
        //    try
        //    {
        //        var selectedMachine = _sessionService.GetSelectedMachine();
        //        if (selectedMachine == null)
        //        {
        //            return ServiceResult.Fail("Makine seçilmedi");
        //        }

        //        _logger.LogInformation("Kullanıcı durumu değiştiriliyor: {UserId} -> {Status}", id, isActive);

        //        var result = await _externalApiService.PostAsync<object>(
        //            selectedMachine.ApiAddress,
        //            $"api/v1/users/{id}/status",
        //            new { isActive = isActive }
        //        );

        //        _logger.LogInformation("Kullanıcı durumu başarıyla değiştirildi: {UserId}", id);
        //        return ServiceResult.Success();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Kullanıcı durumu değiştirilirken hata oluştu: {UserId}", id);
        //        return ServiceResult.Fail("Kullanıcı durumu değiştirilemedi");
        //    }
        //}
    }
}
