using App.UI.DTOS;
using App.UI.Helper;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.UI.Services
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
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
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
                var token = await _tokenService.GetTokenAsync();
                string cleanedToken;
                if (!string.IsNullOrEmpty(token))
                {
                    cleanedToken = JwtTokenParser.CleanToken(token);
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cleanedToken);

                }
                else
                {
                    var session = SessionManager.GetSession();

                    if (session != null && !string.IsNullOrEmpty(session.RefreshToken))
                    {
                        var refreshed = await _tokenService.RefreshTokenAsync();
                        if (refreshed)
                        {
                            // Yenileme başarılıysa tekrar token al
                            token = await _tokenService.GetTokenAsync();
                            if (!string.IsNullOrEmpty(token))
                            {
                                cleanedToken = JwtTokenParser.CleanToken(token);
                                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cleanedToken);
                                Log.Information($"Token yenilendi ve Authorization header eklendi");
                            }
                        }
                    }
                }
            }
        }

        public async Task<T> GetAsync<T>(string endpoint, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var response = await _httpClient.GetAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"API isteği başarısız: {response.StatusCode}, Content: {content}");
                }

                try
                {
                    var serviceResult = JsonSerializer.Deserialize<ServiceResult<T>>(content, _jsonOptions);
                    return serviceResult.Data;
                }
                catch (JsonException jex)
                {
                    Log.Error($"JSON Deserialize hatası: {jex.Message}, Content: {content.Substring(0, Math.Min(content.Length, 500))}");

                }
            }
            catch (JsonException ex)
            {
                Log.Error($"JSON Deserialize hatası: {ex.Message}");


            }
            catch (HttpRequestException ex)
            {
                Log.Error($"HTTP isteği hatası: {ex.Message}");

            }
            catch (Exception ex)
            {
                Log.Error($"API isteği sırasında beklenmeyen hata: {ex.Message}");


            }
            return default(T);
        }

        public async Task<T> PostAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"API isteği başarısız: {response.StatusCode}, Content: {responseContent}");
                }

                var serviceResult = JsonSerializer.Deserialize<ServiceResult<T>>(responseContent, _jsonOptions);

                if (serviceResult != null && !serviceResult.Success)
                {
                    Log.Warning($"API başarısız sonuç döndürdü: {serviceResult.Message}");
                }

                return serviceResult.Data;
            }
            catch (JsonException ex)
            {
                Log.Error($"JSON Deserialize hatası: {ex.Message}");

            }
            catch (HttpRequestException ex)
            {
                Log.Error($"HTTP isteği hatası: {ex.Message}");

            }
            catch (Exception ex)
            {
                Log.Error($"API isteği sırasında beklenmeyen hata: {ex.Message}");

            }
            return default(T);
        }


        public async Task<T> PutAsync<T>(string endpoint, object data, bool requiresAuth = true)
        {
            try
            {
                await SetAuthorizationHeader(requiresAuth);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"API isteği başarısız: {response.StatusCode}, Content: {responseContent}");
                }

                var serviceResult = JsonSerializer.Deserialize<ServiceResult<T>>(responseContent, _jsonOptions);

                if (serviceResult != null && !serviceResult.Success)
                {
                    Log.Warning($"API başarısız sonuç döndürdü: {serviceResult.Message}");

                }

                return serviceResult.Data;
            }
            catch (JsonException ex)
            {
                Log.Error($"JSON Deserialize hatası: {ex.Message}");

            }
            catch (HttpRequestException ex)
            {
                Log.Error($"HTTP isteği hatası: {ex.Message}");

            }
            catch (Exception ex)
            {
                Log.Error($"API isteği sırasında beklenmeyen hata: {ex.Message}");

            }
            return default(T);
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

                var responseContent = await response.Content.ReadAsStringAsync();
                Log.Information($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"API isteği başarısız: {response.StatusCode}, Content: {responseContent}");
                }

                if (string.IsNullOrEmpty(responseContent))
                {
                    return default;
                }

                var serviceResult = JsonSerializer.Deserialize<ServiceResult<T>>(responseContent, _jsonOptions);

                if (serviceResult != null && !serviceResult.Success)
                {
                    Log.Warning($"API başarısız sonuç döndürdü: {serviceResult.Message}");

                }

                return serviceResult.Data;
            }
            catch (JsonException ex)
            {
                Log.Error($"JSON Deserialize hatası: {ex.Message}");

            }
            catch (HttpRequestException ex)
            {
                Log.Error($"HTTP isteği hatası: {ex.Message}");

            }
            catch (Exception ex)
            {
                Log.Error($"API isteği sırasında beklenmeyen hata: {ex.Message}");

            }
            return default(T);
        }


    }

}
