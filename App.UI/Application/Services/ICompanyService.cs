using App.UI.Application.DTOS;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Storage;
using System.Text.Json;

namespace App.UI.Application.Services
{
    // ✅ Helper Class 
    internal class ApiParseResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public interface ICompanyService
    {
        Task<ServiceResult<List<CompanyListDto>>> GetListAsync();
        Task<ServiceResult<CompanyDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<CompanyDto>> CreateAsync(CompanyCreateDto createDto);
        Task<ServiceResult<CompanyDto>> UpdateAsync(CompanyUpdateDto updateDto);
        Task<ServiceResult> DeleteAsync(DeleteDto deleteDto);
        Task<ServiceResult<List<DistrictDto>>> GetDistrictsAsync();
    }

    public class CompanyService : ICompanyService
    {
        private readonly IExternalApiService _externalApiService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<CompanyService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CompanyService(
            IExternalApiService externalApiService,
            ISessionService sessionService,
            ILogger<CompanyService> logger)
        {
            _externalApiService = externalApiService;
            _sessionService = sessionService;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        // ✅ Helper: API Response'unda validation hatalarını parse et
        private ApiParseResult ParseApiResponse(string jsonString)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    var root = doc.RootElement;

                    bool isSuccess = false;
                    string message = "Bilinmeyen hata oluştu";

                    // isSuccess property'sini kontrol et
                    if (root.TryGetProperty("isSuccess", out var isSuccessElement))
                    {
                        isSuccess = isSuccessElement.GetBoolean();
                    }

                    // message property'sini kontrol et
                    if (root.TryGetProperty("message", out var messageElement))
                    {
                        message = messageElement.GetString() ?? message;
                    }

                    // errorMessage array'ini kontrol et (validation hataları için)
                    if (root.TryGetProperty("errorMessage", out var errorElement) &&
                        errorElement.ValueKind == JsonValueKind.Array)
                    {
                        var errors = new List<string>();
                        foreach (var error in errorElement.EnumerateArray())
                        {
                            var errorStr = error.GetString();
                            if (!string.IsNullOrEmpty(errorStr))
                                errors.Add(errorStr);
                        }

                        if (errors.Count > 0)
                        {
                            message = string.Join(", ", errors);
                        }
                    }

                    return new ApiParseResult { IsSuccess = isSuccess, Message = message };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON parse hatası");
                return new ApiParseResult { IsSuccess = false, Message = $"Hata: {ex.Message}" };
            }
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
        private ServiceResult<dynamic> GetSelectedMachine()
        {
            var selectedMachine = _sessionService.GetSelectedMachine();
            if (selectedMachine == null)
            {
                return ServiceResult<dynamic>.Fail("Makine seçilmedi");
            }
            return ServiceResult<dynamic>.Success(selectedMachine);
        }

        public async Task<ServiceResult<List<CompanyListDto>>> GetListAsync()
        {
            try
            {
                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<List<CompanyListDto>>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<List<CompanyListDto>>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şirketler listesi alınıyor");

                var response = await _externalApiService.GetWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/company",
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                    if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                    {
                        if (dataElement.TryGetProperty("list", out JsonElement listElement))
                        {
                            var companies = JsonSerializer.Deserialize<List<CompanyListDto>>(
                            listElement.GetRawText(),
                            _jsonOptions);

                            _logger.LogInformation("Şirket listesi başarıyla alındı: {Count}", companies?.Count ?? 0);
                            return ServiceResult<List<CompanyListDto>>.Success(companies ?? new List<CompanyListDto>());
                        }
                    }
                }
                else
                {
                    return ServiceResult<List<CompanyListDto>>.Fail(result.Message);
                }

                _logger.LogWarning("Şirket listesi yüklenemedi");
                return ServiceResult<List<CompanyListDto>>.Fail("Şirket listesi yüklenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şirket listesi alınırken hata oluştu");
                return ServiceResult<List<CompanyListDto>>.Fail($"Hata: {ex.Message}");
            }
        }

        public async Task<ServiceResult<CompanyDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<CompanyDto>.Fail("Geçerli bir şirket ID'si gereklidir");

                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<CompanyDto>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<CompanyDto>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şirket detayı alınıyor: {CompanyId}", id);

                var response = await _externalApiService.GetWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    $"identity/company/{id}",
                    tokenResult.Data
                );

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                    if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                    {
                        JsonElement companyElement;

                        if (dataElement.TryGetProperty("item", out var itemElement))
                        {
                            companyElement = itemElement;
                        }
                        else
                        {
                            companyElement = dataElement;
                        }
                        var company = JsonSerializer.Deserialize<CompanyDto>(
                            companyElement.GetRawText(),
                            _jsonOptions);

                        _logger.LogInformation("Şirket detayı alındı: {CompanyId}", id);
                        return ServiceResult<CompanyDto>.Success(company);
                    }
                }

                _logger.LogWarning("Şirket bulunamadı: {CompanyId}", id);
                return ServiceResult<CompanyDto>.Fail("Şirket bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şirket detayı alınırken hata: {CompanyId}", id);
                return ServiceResult<CompanyDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ CREATE - Validation hataları yakalanıyor
        public async Task<ServiceResult<CompanyDto>> CreateAsync(CompanyCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                    return ServiceResult<CompanyDto>.Fail("Şirket verisi boş olamaz");

                if (string.IsNullOrWhiteSpace(createDto.Code))
                    return ServiceResult<CompanyDto>.Fail("Şirket kodu zorunludur");

                if (string.IsNullOrWhiteSpace(createDto.Name))
                    return ServiceResult<CompanyDto>.Fail("Şirket adı zorunludur");

                if (string.IsNullOrWhiteSpace(createDto.TaxNumber))
                    return ServiceResult<CompanyDto>.Fail("Vergi numarası zorunludur");

                if (createDto.DistrictId == Guid.Empty)
                    return ServiceResult<CompanyDto>.Fail("İlçe seçimi zorunludur");

                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<CompanyDto>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<CompanyDto>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Yeni şirket oluşturuluyor: {CompanyName}", createDto.Name);

                var response = await _externalApiService.PostWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/company",
                    createDto,
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                    if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                    {
                        var company = JsonSerializer.Deserialize<CompanyDto>(
                            dataElement.GetRawText(),
                            _jsonOptions);

                        _logger.LogInformation("Şirket oluşturuldu: {CompanyName}", createDto.Name);
                        return ServiceResult<CompanyDto>.Success(company);
                    }
                }
                else
                {
                    return ServiceResult<CompanyDto>.Fail(result.Message);
                }

                _logger.LogWarning("Şirket oluşturulamadı");
                return ServiceResult<CompanyDto>.Fail("Şirket oluşturulamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şirket oluşturulurken hata: {CompanyName}", createDto?.Name);
                return ServiceResult<CompanyDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ UPDATE - Validation hataları yakalanıyor
        public async Task<ServiceResult<CompanyDto>> UpdateAsync(CompanyUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return ServiceResult<CompanyDto>.Fail("Şirket verisi boş olamaz");

                if (updateDto.Id == Guid.Empty)
                    return ServiceResult<CompanyDto>.Fail("Geçerli bir şirket ID'si gereklidir");

                if (string.IsNullOrWhiteSpace(updateDto.Code))
                    return ServiceResult<CompanyDto>.Fail("Şirket kodu zorunludur");

                if (string.IsNullOrWhiteSpace(updateDto.Name))
                    return ServiceResult<CompanyDto>.Fail("Şirket adı zorunludur");

                if (string.IsNullOrWhiteSpace(updateDto.TaxNumber))
                    return ServiceResult<CompanyDto>.Fail("Vergi numarası zorunludur");

                if (updateDto.DistrictId == Guid.Empty)
                    return ServiceResult<CompanyDto>.Fail("İlçe seçimi zorunludur");

                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<CompanyDto>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<CompanyDto>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şirket güncelleniyor: {CompanyId}", updateDto.Id);

                var response = await _externalApiService.PutWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/company",
                    updateDto,
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                    if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                    {
                        var company = JsonSerializer.Deserialize<CompanyDto>(
                            dataElement.GetRawText(),
                            _jsonOptions);

                        _logger.LogInformation("Şirket güncellendi: {CompanyId}", updateDto.Id);
                        return ServiceResult<CompanyDto>.Success(company);
                    }
                }
                else
                {
                    return ServiceResult<CompanyDto>.Fail(result.Message);
                }

                _logger.LogWarning("Şirket güncellenemedi: {CompanyId}", updateDto.Id);
                return ServiceResult<CompanyDto>.Fail("Şirket güncellenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şirket güncellenirken hata: {CompanyId}", updateDto?.Id);
                return ServiceResult<CompanyDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ DELETE - Validation hataları yakalanıyor
        public async Task<ServiceResult> DeleteAsync(DeleteDto deleteDto)
        {
            try
            {
                if (deleteDto == null)
                    return ServiceResult.Fail("Geçerli bir şirket ID'si gereklidir");

                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şirket siliniyor: {CompanyId}", deleteDto.Id);

                var response = await _externalApiService.DeleteWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/company",
                    deleteDto,
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    _logger.LogInformation("Şirket başarıyla silindi: {CompanyId}", deleteDto.Id);
                    return ServiceResult.Success();
                }
                else
                {
                    return ServiceResult.Fail(result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şirket silinirken hata: {CompanyId}", deleteDto?.Id);
                return ServiceResult.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ GET DISTRICTS - Validation hataları yakalanıyor
        public async Task<ServiceResult<List<DistrictDto>>> GetDistrictsAsync()
        {
            try
            {
                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<List<DistrictDto>>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<List<DistrictDto>>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("İlçeler listesi alınıyor");

                var response = await _externalApiService.GetWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/district",
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                    if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                    {
                        if (dataElement.TryGetProperty("list", out JsonElement listElement))
                        {
                            var districts = JsonSerializer.Deserialize<List<DistrictDto>>(
                            listElement.GetRawText(),
                            _jsonOptions);

                            _logger.LogInformation("İlçe listesi başarıyla alındı: {Count}", districts?.Count ?? 0);
                            return ServiceResult<List<DistrictDto>>.Success(districts ?? new List<DistrictDto>());
                        }
                    }
                }
                else
                {
                    return ServiceResult<List<DistrictDto>>.Fail(result.Message);
                }

                _logger.LogWarning("İlçe listesi yüklenemedi");
                return ServiceResult<List<DistrictDto>>.Fail("İlçe listesi yüklenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İlçe listesi alınırken hata oluştu");
                return ServiceResult<List<DistrictDto>>.Fail($"Hata: {ex.Message}");
            }
        }
    }
}