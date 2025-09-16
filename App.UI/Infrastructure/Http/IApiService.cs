using App.UI.Application.DTOS;
using App.UI.Helper;
using App.UI.Infrastructure.Storage;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace App.UI.Infrastructure.Http
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string endpoint, bool requiresAuth = true);
        Task<T> PostAsync<T>(string endpoint, object data, bool requiresAuth = true);
        Task<T> PutAsync<T>(string endpoint, object data, bool requiresAuth = true);
        Task<T> DeleteAsync<T>(string endpoint, object data = null, bool requiresAuth = true);

        // ServiceResult için özelleştirilmiş metodlar
        Task<ServiceResult> GetServiceResultAsync(string endpoint, bool requiresAuth = true);
        Task<ServiceResult> PostServiceResultAsync(string endpoint, object data, bool requiresAuth = true);
        Task<ServiceResult> PutServiceResultAsync(string endpoint, object data, bool requiresAuth = true);
        Task<ServiceResult> DeleteServiceResultAsync(string endpoint, object data = null, bool requiresAuth = true);

        Task<ServiceResult<T>> GetServiceResultAsync<T>(string endpoint, bool requiresAuth = true);
        Task<ServiceResult<T>> PostServiceResultAsync<T>(string endpoint, object data, bool requiresAuth = true);
        Task<ServiceResult<T>> PutServiceResultAsync<T>(string endpoint, object data, bool requiresAuth = true);
        Task<ServiceResult<T>> DeleteServiceResultAsync<T>(string endpoint, object data = null, bool requiresAuth = true);
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient, ITokenService tokenService, ISessionService sessionService, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _logger = logger;
            _sessionService = sessionService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private async Task SetAuthorizationHeader(bool requiresAuth)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");

            if (requiresAuth)
            {
                var userSession = _sessionService.GetUserSession();

                if (userSession != null && !string.IsNullOrEmpty(userSession.AccessToken))
                {
                    if (_sessionService.IsAuthenticated())
                    {
                        var cleanedToken = JwtTokenParser.CleanToken(userSession.AccessToken);
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cleanedToken);
                    }
                }
            }
        }

        #region Generic Methods (Backward Compatibility)

        public async Task<T> GetAsync<T>(string endpoint, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var response = await _httpClient.GetAsync(endpoint);
                return await ProcessResponse<T>(response, endpoint, "GET");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return default;
            }
        }

        public async Task<T> PostAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                return await ProcessResponse<T>(response, endpoint, "POST");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return default;
            }
        }

        public async Task<T> PutAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);

                return await ProcessResponse<T>(response, endpoint, "PUT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUT isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return default;
            }
        }

        public async Task<T> DeleteAsync<T>(string endpoint, object data = null, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);

                HttpResponseMessage response;
                if (data != null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                    var json = JsonSerializer.Serialize(data, _jsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await _httpClient.SendAsync(request);
                }
                else
                {
                    response = await _httpClient.DeleteAsync(endpoint);
                }

                return await ProcessResponse<T>(response, endpoint, "DELETE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return default;
            }
        }

        #endregion

        #region ServiceResult Specific Methods

        public async Task<ServiceResult> GetServiceResultAsync(string endpoint, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var response = await _httpClient.GetAsync(endpoint);
                return await ProcessServiceResultResponse(response, endpoint, "GET");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET ServiceResult isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> PostServiceResultAsync(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                return await ProcessServiceResultResponse(response, endpoint, "POST");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST ServiceResult isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> PutServiceResultAsync(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                return await ProcessServiceResultResponse(response, endpoint, "PUT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUT ServiceResult isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> DeleteServiceResultAsync(string endpoint, object data = null, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);

                HttpResponseMessage response;
                if (data != null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                    var json = JsonSerializer.Serialize(data, _jsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await _httpClient.SendAsync(request);
                }
                else
                {
                    response = await _httpClient.DeleteAsync(endpoint);
                }

                return await ProcessServiceResultResponse(response, endpoint, "DELETE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE ServiceResult isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<T>> GetServiceResultAsync<T>(string endpoint, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var response = await _httpClient.GetAsync(endpoint);
                return await ProcessServiceResultResponse<T>(response, endpoint, "GET");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET ServiceResult<T> isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult<T>.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<T>> PostServiceResultAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                return await ProcessServiceResultResponse<T>(response, endpoint, "POST");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST ServiceResult<T> isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult<T>.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<T>> PutServiceResultAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                return await ProcessServiceResultResponse<T>(response, endpoint, "PUT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUT ServiceResult<T> isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult<T>.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<T>> DeleteServiceResultAsync<T>(string endpoint, object data = null, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);

                HttpResponseMessage response;
                if (data != null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                    var json = JsonSerializer.Serialize(data, _jsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await _httpClient.SendAsync(request);
                }
                else
                {
                    response = await _httpClient.DeleteAsync(endpoint);
                }

                return await ProcessServiceResultResponse<T>(response, endpoint, "DELETE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE ServiceResult<T> isteği sırasında hata oluştu: {Endpoint}", endpoint);
                return ServiceResult<T>.Fail($"İstek sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Response Processing Methods

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response, string endpoint, string method)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Method} isteği başarısız: {StatusCode} - {Endpoint} - Content: {Content}",
                    method, response.StatusCode, endpoint, responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("401 Unauthorized alındı, token problemi olabilir");
                }

                return default;
            }

            if (string.IsNullOrEmpty(responseContent))
            {
                return default;
            }

            try
            {
                // ServiceResult türü kontrolü (generic olmayan)
                if (typeof(T) == typeof(ServiceResult))
                {
                    var serviceResult = JsonSerializer.Deserialize<ServiceResult>(responseContent, _jsonOptions);
                    return (T)(object)serviceResult;
                }

                // ServiceResult<T> türü kontrolü (generic olan)
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ServiceResult<>))
                {
                    return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                }

                // Diğer durumlarda API'den ServiceResult<T> beklenir ve içindeki Data döndürülür
                var wrappedResult = JsonSerializer.Deserialize<ServiceResult<T>>(responseContent, _jsonOptions);

                if (wrappedResult != null && wrappedResult.IsSuccess)
                {
                    return wrappedResult.Data;
                }
                else
                {
                    _logger.LogWarning("API başarısız sonuç döndürdü: {Messages}",
                        wrappedResult?.ErrorMessage != null ? string.Join(", ", wrappedResult.ErrorMessage) : "Bilinmeyen hata");
                    return default;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON Deserialize hatası: {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);
                return default;
            }
        }

        private async Task<ServiceResult> ProcessServiceResultResponse(HttpResponseMessage response, string endpoint, string method)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Method} ServiceResult isteği başarısız: {StatusCode} - {Endpoint} - Content: {Content}",
                    method, response.StatusCode, endpoint, responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);

                // HTTP status koduna göre ServiceResult oluştur
                var errorMessage = TryParseErrorMessage(responseContent) ?? $"HTTP {(int)response.StatusCode} hata kodu alındı";
                return ServiceResult.Fail(errorMessage, response.StatusCode);
            }

            if (string.IsNullOrEmpty(responseContent))
            {
                return ServiceResult.Success();
            }

            try
            {
                var serviceResult = JsonSerializer.Deserialize<ServiceResult>(responseContent, _jsonOptions);

                if (serviceResult == null)
                {
                    return ServiceResult.Fail("API'den geçersiz yanıt alındı", HttpStatusCode.BadRequest);
                }

                // Status code'u response'dan set et
                serviceResult.Status = response.StatusCode;
                return serviceResult;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "ServiceResult JSON Deserialize hatası: {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);
                return ServiceResult.Fail($"JSON ayrıştırma hatası: {ex.Message}", HttpStatusCode.BadRequest);
            }
        }

        private async Task<ServiceResult<T>> ProcessServiceResultResponse<T>(HttpResponseMessage response, string endpoint, string method)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Method} ServiceResult<T> isteği başarısız: {StatusCode} - {Endpoint} - Content: {Content}",
                    method, response.StatusCode, endpoint, responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);

                // HTTP status koduna göre ServiceResult<T> oluştur
                var errorMessage = TryParseErrorMessage(responseContent) ?? $"HTTP {(int)response.StatusCode} hata kodu alındı";
                return ServiceResult<T>.Fail(errorMessage, response.StatusCode);
            }

            if (string.IsNullOrEmpty(responseContent))
            {
                return ServiceResult<T>.Success(default, response.StatusCode);
            }

            try
            {
                var serviceResult = JsonSerializer.Deserialize<ServiceResult<T>>(responseContent, _jsonOptions);

                if (serviceResult == null)
                {
                    return ServiceResult<T>.Fail("API'den geçersiz yanıt alındı", HttpStatusCode.BadRequest);
                }

                // Status code'u response'dan set et
                serviceResult.Status = response.StatusCode;

                return serviceResult;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "ServiceResult<T> JSON Deserialize hatası: {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);
                return ServiceResult<T>.Fail($"JSON ayrıştırma hatası: {ex.Message}", HttpStatusCode.BadRequest);
            }
        }

        private string? TryParseErrorMessage(string responseContent)
        {
            try
            {
                // ServiceResult formatında hata mesajını çıkarmaya çalış
                using var document = JsonDocument.Parse(responseContent);
                if (document.RootElement.TryGetProperty("errorMessage", out var errorElement) &&
                    errorElement.ValueKind == JsonValueKind.Array)
                {
                    var errors = new List<string>();
                    foreach (var error in errorElement.EnumerateArray())
                    {
                        if (error.ValueKind == JsonValueKind.String)
                        {
                            errors.Add(error.GetString());
                        }
                    }
                    return errors.Count > 0 ? string.Join(", ", errors) : null;
                }
            }
            catch
            {
                // Hata ayrıştırmada sorun varsa null dön
            }
            return null;
        }

        #endregion
    }
}