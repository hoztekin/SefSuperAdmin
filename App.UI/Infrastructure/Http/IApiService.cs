using App.UI.Application.DTOS;
using App.UI.Helper;
using App.UI.Infrastructure.Storage;
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
                        _logger.LogDebug("Authorization header eklendi");
                    }
                    else
                    {
                        // Token süresi dolmuş, yenilemeyi dene
                        _logger.LogInformation("Token süresi dolmuş, yenileme deneniyor...");
                        var refreshed = await _tokenService.RefreshTokenAsync();

                        if (refreshed)
                        {
                            var newUserSession = _sessionService.GetUserSession();
                            if (newUserSession != null && !string.IsNullOrEmpty(newUserSession.AccessToken))
                            {
                                var cleanedToken = JwtTokenParser.CleanToken(newUserSession.AccessToken);
                                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cleanedToken);
                                _logger.LogInformation("Token yenilendi ve Authorization header eklendi");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Token yenilenemedi");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Session veya AccessToken bulunamadı");
                }
            }
        }

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

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response, string endpoint, string method)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Method} isteği başarısız: {StatusCode} - {Endpoint} - Content: {Content}",
                    method, response.StatusCode, endpoint, responseContent.Substring(0, Math.Min(responseContent.Length, 500)));

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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
                // Eğer T bir ServiceResult ise direkt deserialize et
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ServiceResult<>))
                {
                    return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                }

                // Değilse ServiceResult içindeki Data'yı çıkar
                var serviceResult = JsonSerializer.Deserialize<ServiceResult<T>>(responseContent, _jsonOptions);

                if (serviceResult != null && serviceResult.Success)
                {
                    return serviceResult.Data;
                }
                else
                {
                    _logger.LogWarning("API başarısız sonuç döndürdü: {Message}", serviceResult?.Message);
                    return default;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON Deserialize hatası: {Content}", responseContent.Substring(0, Math.Min(responseContent.Length, 500)));
                return default;
            }
        }


    }

}
