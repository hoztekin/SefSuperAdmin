using App.UI.Application.DTOS;
using App.UI.Infrastructure.Storage;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace App.UI.Infrastructure.ExternalApi
{
    public interface IExternalApiService
    {
        Task<ExternalApiHealthResponse> CheckHealthAsync(string apiAddress);
        Task<ExternalApiLoginResponse> LoginAsync(string apiAddress, string username = "Admin", string password = "Admin1234");

        // Token ile CRUD metodları
        Task<HttpResponseMessage> GetWithTokenAsync(string apiAddress, string endpoint, string token);
        Task<HttpResponseMessage> PostWithTokenAsync(string apiAddress, string endpoint, object data, string token);
        Task<HttpResponseMessage> PutWithTokenAsync(string apiAddress, string endpoint, object data, string token);
        Task<HttpResponseMessage> DeleteWithTokenAsync(string apiAddress, string endpoint, object data, string token);

        Task<bool> RefreshTokenAsync(string apiAddress);
    }

    public class ExternalApiService : IExternalApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ISessionService _sessionService;

        public ExternalApiService(IHttpClientFactory httpClientFactory, ILogger<ExternalApiService> logger, ISessionService sessionService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _sessionService = sessionService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
        }

        public async Task<ExternalApiHealthResponse> CheckHealthAsync(string apiAddress)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Health check endpoint'ini dene (login gerektirmez)
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
                    _logger.LogInformation("Health check başarılı: {ApiAddress} - {ResponseTime}ms",
                        apiAddress, healthResponse.ResponseTime);
                }
                else
                {
                    healthResponse.Message = $"API yanıt vermiyor: {response.StatusCode}";
                    _logger.LogWarning("Health check başarısız: {ApiAddress} - {StatusCode}",
                        apiAddress, response.StatusCode);
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

        public async Task<ExternalApiLoginResponse> LoginAsync(string apiAddress, string username = "Admin", string password = "Admin1234")
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var loginUrl = $"{apiAddress.TrimEnd('/')}/Public/Auth/Login";
                var loginRequest = new ExternalApiLoginRequest
                {
                    UserName = username,
                    Password = password
                };

                var json = JsonSerializer.Serialize(loginRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(loginUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Login başarısız: {ApiAddress} - {StatusCode} - {ResponseContent}",
                        apiAddress, response.StatusCode, responseContent);

                    return new ExternalApiLoginResponse
                    {
                        Success = false,
                        Message = $"Login başarısız: {response.StatusCode}"
                    };
                }

                // ---- JSON Parse ----
                var loginResponse = new ExternalApiLoginResponse
                {
                    Success = true,
                    Message = "Login başarılı"
                };

                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("token", out var tokenElement))
                    {
                        loginResponse.AccessToken = tokenElement.GetProperty("access_token").GetString() ?? string.Empty;
                        loginResponse.RefreshToken = tokenElement.GetProperty("refresh_token").GetString() ?? string.Empty;

                        if (tokenElement.TryGetProperty("expires_in", out var expInElement))
                        {
                            loginResponse.ExpiresIn = expInElement.GetInt32();
                            loginResponse.ExpiresAt = DateTime.UtcNow.AddSeconds(loginResponse.ExpiresIn);
                        }
                    }
                }

                _logger.LogInformation("Login başarılı: {ApiAddress} - Token alındı: {HasToken}",
                    apiAddress, !string.IsNullOrEmpty(loginResponse.AccessToken));

                // ---- Session'a Kaydet ----
                if (!string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    _logger.LogDebug("Token session'a kaydediliyor: ApiAddress={ApiAddress}, ExpiresAt={ExpiresAt}",  apiAddress, loginResponse.ExpiresAt);

                    _sessionService.SaveMachineApiToken(
                        apiAddress,
                        loginResponse.AccessToken,
                        loginResponse.ExpiresAt,
                        loginResponse.RefreshToken
                    );
                }

                return loginResponse;
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

        public async Task<HttpResponseMessage> GetWithTokenAsync(string apiAddress, string endpoint, string token)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var fullUrl = $"{apiAddress.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                var response = await httpClient.GetAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("External API GET başarılı: {Url}", fullUrl);
                }
                else
                {
                    _logger.LogWarning("External API GET başarısız: {Url} - {StatusCode}", fullUrl, response.StatusCode);
                }

                return response; // Raw HttpResponseMessage döndür
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API GET hatası: {ApiAddress}/{Endpoint}", apiAddress, endpoint);
                throw; // Exception'ı yukarı fırlat
            }
        }

        public async Task<HttpResponseMessage> PostWithTokenAsync(string apiAddress, string endpoint, object data, string token)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var fullUrl = $"{apiAddress.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(fullUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("External API Post başarılı: {Url}", fullUrl);
                }
                else
                {
                    _logger.LogWarning("External API Post başarısız: {Url} - {StatusCode}", fullUrl, response.StatusCode);
                }

                return response; // Raw HttpResponseMessage döndür
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API POST hatası: {ApiAddress}/{Endpoint}", apiAddress, endpoint);
                throw new HttpRequestException($"External API POST hatası: {apiAddress}/{endpoint}", ex);
            }
        }

        public async Task<HttpResponseMessage> PutWithTokenAsync(string apiAddress, string endpoint, object data, string token)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var fullUrl = $"{apiAddress.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(fullUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("External API Put başarılı: {Url}", fullUrl);
                }
                else
                {
                    _logger.LogWarning("External API Put başarısız: {Url} - {StatusCode}", fullUrl, response.StatusCode);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API PUT hatası: {ApiAddress}/{Endpoint}", apiAddress, endpoint);
                throw new HttpRequestException($"External API PUT hatası: {apiAddress}/{endpoint}", ex);
            }
        }

        public async Task<HttpResponseMessage> DeleteWithTokenAsync(string apiAddress, string endpoint, object data, string token)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("ExternalApiClient");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var fullUrl = $"{apiAddress.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);

                if (data != null)
                {
                    var json = JsonSerializer.Serialize(data, _jsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("External API DELETE başarılı: {Url}", fullUrl);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("External API DELETE başarısız: {Url} - {StatusCode} - {Error}",
                        fullUrl, response.StatusCode, errorContent);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API DELETE hatası: {ApiAddress}/{Endpoint}", apiAddress, endpoint);
                throw new HttpRequestException($"External API DELETE hatası: {apiAddress}/{endpoint}", ex);
            }
        }

        public async Task<bool> RefreshTokenAsync(string apiAddress)
        {
            try
            {
                var loginResponse = await LoginAsync(apiAddress);
                return loginResponse.Success && !string.IsNullOrEmpty(loginResponse.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token yenileme hatası: {ApiAddress}", apiAddress);
                return false;
            }
        }
    }
}
