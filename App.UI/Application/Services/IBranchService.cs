using App.UI.Application.DTOS;
using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Storage;
using System.Text.Json;

namespace App.UI.Application.Services
{
    public interface IBranchService
    {
        Task<ServiceResult<List<BranchListDto>>> GetListAsync();
        Task<ServiceResult<BranchDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<BranchDto>> CreateAsync(BranchCreateDto createDto);
        Task<ServiceResult<BranchDto>> UpdateAsync(BranchUpdateDto updateDto);
        Task<ServiceResult> DeleteAsync(DeleteDto deleteDto);
        Task<ServiceResult<List<DistrictDto>>> GetDistrictsAsync();
    }
    public class BranchService : IBranchService
    {
        private readonly IExternalApiService _externalApiService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<BranchService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public BranchService(
            IExternalApiService externalApiService,
            ISessionService sessionService,
            ILogger<BranchService> logger)
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

                    // Hata detaylarını kontrol et
                    if (!isSuccess && root.TryGetProperty("errors", out var errorsElement))
                    {
                        var errorMessages = new List<string>();
                        foreach (var property in errorsElement.EnumerateObject())
                        {
                            foreach (var error in property.Value.EnumerateArray())
                            {
                                var errorStr = error.GetString();
                                if (!string.IsNullOrEmpty(errorStr))
                                    errorMessages.Add(errorStr);
                            }
                        }
                        if (errorMessages.Any())
                        {
                            message = string.Join(" | ", errorMessages);
                        }
                    }

                    return new ApiParseResult { IsSuccess = isSuccess, Message = message };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Response parse edilirken hata");
                return new ApiParseResult { IsSuccess = false, Message = "Hata: " + ex.Message };
            }
        }

        // ✅ Helper: Token al veya login yap (CompanyService'den aldı)
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

        // ✅ Helper: Selected Machine al (CompanyService'den aldı)
        private ServiceResult<dynamic> GetSelectedMachine()
        {
            var selectedMachine = _sessionService.GetSelectedMachine();
            if (selectedMachine == null)
            {
                return ServiceResult<dynamic>.Fail("Makine seçilmedi");
            }
            return ServiceResult<dynamic>.Success(selectedMachine);
        }

        // ✅ GET LIST
        public async Task<ServiceResult<List<BranchListDto>>> GetListAsync()
        {
            try
            {
                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<List<BranchListDto>>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<List<BranchListDto>>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şubeler listeleniyor");

                var response = await _externalApiService.GetWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/branch",
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                    if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                    {
                        var listElement = dataElement;

                        // data bir object ise, içinde "list" property'si arıyoruz
                        if (dataElement.ValueKind == JsonValueKind.Object)
                        {
                            if (dataElement.TryGetProperty("list", out var foundList))
                            {
                                listElement = foundList;
                            }
                        }

                        var branches = JsonSerializer.Deserialize<List<BranchListDto>>(
                            listElement.GetRawText(),
                            _jsonOptions);

                        _logger.LogInformation("Toplam {Count} şube listelendi", branches?.Count ?? 0);
                        return ServiceResult<List<BranchListDto>>.Success(branches ?? new List<BranchListDto>());
                    }
                }

                _logger.LogWarning("Şubeler listelenemedi");
                return ServiceResult<List<BranchListDto>>.Fail(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şubeler listelenirken hata");
                return ServiceResult<List<BranchListDto>>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ GET BY ID
        public async Task<ServiceResult<BranchDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<BranchDto>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<BranchDto>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şube detayı alınıyor: {BranchId}", id);

                var response = await _externalApiService.GetWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    $"identity/branch/{id}",
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                    if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                    {
                        var branchElement = dataElement;
                        if (dataElement.ValueKind == JsonValueKind.Object)
                        {
                            if (dataElement.TryGetProperty("item", out var itemElement))
                            {
                                branchElement = itemElement;
                            }
                        }

                        var branch = JsonSerializer.Deserialize<BranchDto>(
                            branchElement.GetRawText(),
                            _jsonOptions);

                        _logger.LogInformation("Şube detayı alındı: {BranchId}", id);
                        return ServiceResult<BranchDto>.Success(branch);
                    }
                }

                _logger.LogWarning("Şube bulunamadı: {BranchId}", id);
                return ServiceResult<BranchDto>.Fail("Şube bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şube detayı alınırken hata: {BranchId}", id);
                return ServiceResult<BranchDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ CREATE
        public async Task<ServiceResult<BranchDto>> CreateAsync(BranchCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                    return ServiceResult<BranchDto>.Fail("Şube verisi boş olamaz");

                if (string.IsNullOrWhiteSpace(createDto.Code))
                    return ServiceResult<BranchDto>.Fail("Şube kodu zorunludur");

                if (string.IsNullOrWhiteSpace(createDto.Name))
                    return ServiceResult<BranchDto>.Fail("Şube adı zorunludur");

                if (string.IsNullOrWhiteSpace(createDto.Phone))
                    return ServiceResult<BranchDto>.Fail("Telefon zorunludur");

                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<BranchDto>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<BranchDto>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Yeni şube oluşturuluyor: {BranchName}", createDto.Name);

                var response = await _externalApiService.PostWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/branch",
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
                        var branchElement = dataElement;
                        if (dataElement.ValueKind == JsonValueKind.Object)
                        {
                            if (dataElement.TryGetProperty("item", out var itemElement))
                            {
                                branchElement = itemElement;
                            }
                        }

                        var branch = JsonSerializer.Deserialize<BranchDto>(
                            branchElement.GetRawText(),
                            _jsonOptions);

                        _logger.LogInformation("Şube oluşturuldu: {BranchName}", createDto.Name);
                        return ServiceResult<BranchDto>.Success(branch);
                    }
                }
                else
                {
                    return ServiceResult<BranchDto>.Fail(result.Message);
                }

                _logger.LogWarning("Şube oluşturulamadı");
                return ServiceResult<BranchDto>.Fail("Şube oluşturulamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şube oluşturulurken hata: {BranchName}", createDto?.Name);
                return ServiceResult<BranchDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ UPDATE
        public async Task<ServiceResult<BranchDto>> UpdateAsync(BranchUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return ServiceResult<BranchDto>.Fail("Şube verisi boş olamaz");

                if (updateDto.Id == Guid.Empty)
                    return ServiceResult<BranchDto>.Fail("Şube ID'si boş olamaz");

                if (string.IsNullOrWhiteSpace(updateDto.Code))
                    return ServiceResult<BranchDto>.Fail("Şube kodu zorunludur");

                if (string.IsNullOrWhiteSpace(updateDto.Name))
                    return ServiceResult<BranchDto>.Fail("Şube adı zorunludur");

                if (string.IsNullOrWhiteSpace(updateDto.Phone))
                    return ServiceResult<BranchDto>.Fail("Telefon zorunludur");

                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult<BranchDto>.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult<BranchDto>.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şube güncelleniyor: {BranchId}", updateDto.Id);

                var response = await _externalApiService.PutWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    $"identity/branch/{updateDto.Id}",
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
                        var branchElement = dataElement;
                        if (dataElement.ValueKind == JsonValueKind.Object)
                        {
                            if (dataElement.TryGetProperty("item", out var itemElement))
                            {
                                branchElement = itemElement;
                            }
                        }

                        var branch = JsonSerializer.Deserialize<BranchDto>(
                            branchElement.GetRawText(),
                            _jsonOptions);

                        _logger.LogInformation("Şube güncellendi: {BranchId}", updateDto.Id);
                        return ServiceResult<BranchDto>.Success(branch);
                    }
                }
                else
                {
                    return ServiceResult<BranchDto>.Fail(result.Message);
                }

                _logger.LogWarning("Şube güncellenemedi: {BranchId}", updateDto.Id);
                return ServiceResult<BranchDto>.Fail("Şube güncellenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şube güncellenirken hata: {BranchId}", updateDto?.Id);
                return ServiceResult<BranchDto>.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ DELETE
        public async Task<ServiceResult> DeleteAsync(DeleteDto deleteDto)
        {
            try
            {
                if (deleteDto == null)
                    return ServiceResult.Fail("Şube ID'si boş olamaz");

                var machineResult = GetSelectedMachine();
                if (!machineResult.IsSuccess)
                    return ServiceResult.Fail(machineResult.ErrorMessage);

                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess)
                    return ServiceResult.Fail(tokenResult.ErrorMessage);

                _logger.LogInformation("Şube siliniyor: {BranchId}", deleteDto.Id);



                var response = await _externalApiService.DeleteWithTokenAsync(
                                    machineResult.Data.ApiAddress,
                                    "identity/branch",
                                    deleteDto,
                                    tokenResult.Data
                                    );

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = ParseApiResponse(jsonString);

                if (response.IsSuccessStatusCode && result.IsSuccess)
                {
                    _logger.LogInformation("Şube silindi: {BranchId}", deleteDto.Id);
                    return ServiceResult.Success();
                }

                return ServiceResult.Fail(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şube silinirken hata: {BranchId}", deleteDto?.Id);
                return ServiceResult.Fail($"Hata: {ex.Message}");
            }
        }

        // ✅ GET DISTRICTS
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

                var response = await _externalApiService.GetWithTokenAsync(
                    machineResult.Data.ApiAddress,
                    "identity/district",
                    tokenResult.Data
                );

                var jsonString = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);

                if (apiResponse.TryGetProperty("data", out JsonElement dataElement))
                {
                    var itemElement = dataElement;
                    if (dataElement.ValueKind == JsonValueKind.Object)
                    {
                        if (dataElement.TryGetProperty("items", out var itemsElement))
                        {
                            itemElement = itemsElement;
                        }
                    }

                    var districts = JsonSerializer.Deserialize<List<DistrictDto>>(
                        itemElement.GetRawText(), _jsonOptions);

                    return ServiceResult<List<DistrictDto>>.Success(districts ?? new List<DistrictDto>());
                }

                return ServiceResult<List<DistrictDto>>.Fail("İlçeler yüklenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İlçeler yüklenirken hata");
                return ServiceResult<List<DistrictDto>>.Fail($"Hata: {ex.Message}");
            }
        }
    }
}