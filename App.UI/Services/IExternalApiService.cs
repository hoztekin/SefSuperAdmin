using App.UI.DTOS;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace App.UI.Services
{
    public interface IExternalApiService
    {
        Task<ExternalApiHealthResponse> CheckHealthAsync(string apiAddress);
        Task<ExternalApiLoginResponse> LoginAsync(string apiAddress, string username = "SystemAdmin", string password = "1234");
        Task<T> GetWithTokenAsync<T>(string apiAddress, string endpoint, string token);
        Task<T> PostWithTokenAsync<T>(string apiAddress, string endpoint, object data, string token);
    }

    public class ExternalApiService : IExternalApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ExternalApiService(IHttpClientFactory httpClientFactory, ILogger<ExternalApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<ExternalApiHealthResponse> CheckHealthAsync(string apiAddress)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Health check endpoint'ini dene
                var healthUrl = $"{apiAddress.TrimEnd('/')}/health";

                _logger.LogInformation("Health check başlatılıyor: {HealthUrl}", healthUrl);

                var response = await httpClient.GetAsync(healthUrl);
                stopwatch.Stop();

                var healthResponse = new ExternalApiHealthResponse
                {
                    IsHealthy = response.IsSuccessStatusCode,
                    CheckTime = DateTime.UtcNow,
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds
                };

                if (response.IsSuccessStatusCode)
                {
                    healthResponse.Message = "API başarıyla yanıt verdi";
                    _logger.LogInformation("Health check başarılı: {ApiAddress} - {ResponseTime}ms", apiAddress, healthResponse.ResponseTime);
                }
                else
                {
                    healthResponse.Message = $"API yanıt vermiyor: {response.StatusCode}";
                    _logger.LogWarning("Health check başarısız: {ApiAddress} - {StatusCode}", apiAddress, response.StatusCode);
                }

                return healthResponse;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Health check HTTP hatası: {ApiAddress}", apiAddress);

                return new ExternalApiHealthResponse
                {
                    IsHealthy = false,
                    Message = $"Bağlantı hatası: {ex.Message}",
                    CheckTime = DateTime.UtcNow,
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds
                };
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Health check timeout: {ApiAddress}", apiAddress);

                return new ExternalApiHealthResponse
                {
                    IsHealthy = false,
                    Message = "Bağlantı zaman aşımına uğradı",
                    CheckTime = DateTime.UtcNow,
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Health check genel hatası: {ApiAddress}", apiAddress);

                return new ExternalApiHealthResponse
                {
                    IsHealthy = false,
                    Message = $"Beklenmeyen hata: {ex.Message}",
                    CheckTime = DateTime.UtcNow,
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }

        public async Task<ExternalApiLoginResponse> LoginAsync(string apiAddress, string username = "SystemAdmin", string password = "1234")
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var loginUrl = $"{apiAddress.TrimEnd('/')}/api/v1/Auth/Login";

                var loginRequest = new ExternalApiLoginRequest
                {
                    UserName = username,
                    Password = password
                };

                var json = JsonSerializer.Serialize(loginRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Login isteği gönderiliyor: {LoginUrl} - Kullanıcı: {Username}", loginUrl, username);

                var response = await httpClient.PostAsync(loginUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Yanıtı deserialize et
                    var loginResult = JsonSerializer.Deserialize<dynamic>(responseContent, _jsonOptions);

                    // Farklı response formatları için esnek approach
                    var loginResponse = new ExternalApiLoginResponse
                    {
                        Success = true,
                        Message = "Login başarılı"
                    };

                    // JSON response'undan token bilgilerini çıkar
                    // Bu kısım uzaktaki API'nin response formatına göre düzenlenebilir
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(responseContent);

                        if (doc.RootElement.TryGetProperty("data", out var dataElement))
                        {
                            if (dataElement.TryGetProperty("accessToken", out var tokenElement))
                            {
                                loginResponse.AccessToken = tokenElement.GetString() ?? string.Empty;
                            }

                            if (dataElement.TryGetProperty("refreshToken", out var refreshElement))
                            {
                                loginResponse.RefreshToken = refreshElement.GetString() ?? string.Empty;
                            }

                            if (dataElement.TryGetProperty("accessTokenExpiration", out var expiresElement))
                            {
                                if (DateTime.TryParse(expiresElement.GetString(), out var expiresAt))
                                {
                                    loginResponse.ExpiresAt = expiresAt;
                                    loginResponse.ExpiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;
                                }
                            }
                        }
                        // Alternatif format: token direkt root'ta olabilir
                        else if (doc.RootElement.TryGetProperty("accessToken", out var directTokenElement))
                        {
                            loginResponse.AccessToken = directTokenElement.GetString() ?? string.Empty;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Login response parse edilirken hata: {ResponseContent}", responseContent);
                    }

                    _logger.LogInformation("Login başarılı: {ApiAddress} - Token alındı: {HasToken}",
                        apiAddress, !string.IsNullOrEmpty(loginResponse.AccessToken));

                    return loginResponse;
                }
                else
                {
                    _logger.LogError("Login başarısız: {ApiAddress} - {StatusCode} - {ResponseContent}",
                        apiAddress, response.StatusCode, responseContent);

                    return new ExternalApiLoginResponse
                    {
                        Success = false,
                        Message = $"Login başarısız: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login işlemi sırasında hata: {ApiAddress}", apiAddress);

                return new ExternalApiLoginResponse
                {
                    Success = false,
                    Message = $"Login hatası: {ex.Message}"
                };
            }
        }

        public async Task<T> GetWithTokenAsync<T>(string apiAddress, string endpoint, string token)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var fullUrl = $"{apiAddress.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                var response = await httpClient.GetAsync(fullUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<T>(content, _jsonOptions);
                }

                _logger.LogWarning("External API GET başarısız: {Url} - {StatusCode}", fullUrl, response.StatusCode);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API GET hatası: {ApiAddress}/{Endpoint}", apiAddress, endpoint);
                return default(T);
            }
        }

        public async Task<T> PostWithTokenAsync<T>(string apiAddress, string endpoint, object data, string token)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var fullUrl = $"{apiAddress.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(fullUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("External API POST başarısız: {Url} - {StatusCode}", fullUrl, response.StatusCode);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API POST hatası: {ApiAddress}/{Endpoint}", apiAddress, endpoint);
                return default(T);
            }
        }
    }
}
