using App.UI.Application.DTOS;
using App.UI.Infrastructure.Http;
using App.UI.Presentation.ViewModels;

namespace App.UI.Application.Services
{
    public interface IMachineAppService
    {
        Task<ServiceResult<List<MachineListViewModel>>> GetAllAsync();
        Task<ServiceResult<MachineViewModel>> GetByIdAsync(int id);
        Task<ServiceResult<List<MachineListViewModel>>> GetActiveAsync();
        Task<ServiceResult<CreateMachineViewModel>> CreateAsync(CreateMachineViewModel model);
        Task<ServiceResult> UpdateAsync(UpdateMachineViewModel model);
        Task<ServiceResult> DeleteAsync(int id);
        Task<ServiceResult> SetActiveStatusAsync(int id, bool isActive);
        Task<ServiceResult<bool>> TestApiConnectionAsync(string apiAddress);
    }

    public class MachineAppService : IMachineAppService
    {
        private readonly IApiService _apiService;
        private readonly ILogger<MachineAppService> _logger;
        private const string API_ENDPOINT = "api/v1/Machine";

        public MachineAppService(IApiService apiService, ILogger<MachineAppService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm makineleri getirir
        /// </summary>
        public async Task<ServiceResult<List<MachineListViewModel>>> GetAllAsync()
        {
            try
            {
                var result = await _apiService.GetServiceResultAsync<List<MachineListViewModel>>(API_ENDPOINT);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("Toplam {Count} makine yüklendi", result.Data.Count);
                    return ServiceResult<List<MachineListViewModel>>.Success(result.Data);
                }

                _logger.LogWarning("Makineler yüklenirken API'den başarısız sonuç döndü: {Error}",
                    result.ErrorMessage != null ? string.Join(", ", result.ErrorMessage) : "Bilinmeyen hata");

                return ServiceResult<List<MachineListViewModel>>.Fail(
                    result.ErrorMessage ?? new List<string> { "Makineler yüklenemedi" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makineler yüklenirken hata oluştu");
                return ServiceResult<List<MachineListViewModel>>.Fail("Makineler yüklenirken beklenmeyen bir hata oluştu");
            }
        }

        /// <summary>
        /// Aktif makineleri getirir
        /// </summary>
        public async Task<ServiceResult<List<MachineListViewModel>>> GetActiveAsync()
        {
            try
            {
                var result = await _apiService.GetServiceResultAsync<List<MachineListViewModel>>($"{API_ENDPOINT}/active");

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("Toplam {Count} aktif makine yüklendi", result.Data.Count);
                    return ServiceResult<List<MachineListViewModel>>.Success(result.Data);
                }

                return ServiceResult<List<MachineListViewModel>>.Fail(
                    result.ErrorMessage ?? new List<string> { "Aktif makineler yüklenemedi" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif makineler yüklenirken hata oluştu");
                return ServiceResult<List<MachineListViewModel>>.Fail("Aktif makineler yüklenirken beklenmeyen bir hata oluştu");
            }
        }

        /// <summary>
        /// ID'ye göre makine getirir
        /// </summary>
        public async Task<ServiceResult<MachineViewModel>> GetByIdAsync(int id)
        {
            try
            {
                var result = await _apiService.GetServiceResultAsync<MachineViewModel>($"{API_ENDPOINT}/{id}");

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("Makine yüklendi: {Id} - {BranchName}", result.Data.Id, result.Data.BranchName);
                    return ServiceResult<MachineViewModel>.Success(result.Data);
                }

                return ServiceResult<MachineViewModel>.Fail(
                    result.ErrorMessage ?? new List<string> { "Makine bulunamadı" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine bilgisi alınırken hata oluştu. ID: {Id}", id);
                return ServiceResult<MachineViewModel>.Fail("Makine bilgisi alınırken beklenmeyen bir hata oluştu");
            }
        }

        /// <summary>
        /// Yeni makine oluşturur
        /// </summary>
        public async Task<ServiceResult<CreateMachineViewModel>> CreateAsync(CreateMachineViewModel model)
        {
            try
            {
                var result = await _apiService.PostServiceResultAsync<CreateMachineViewModel>(API_ENDPOINT, model);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Yeni makine oluşturuldu: {Code} - {BranchName}", model.Code, model.BranchName);
                    return ServiceResult<CreateMachineViewModel>.Success(model);
                }

                return ServiceResult<CreateMachineViewModel>.Fail(
                    result.ErrorMessage ?? new List<string> { "Makine oluşturulamadı" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine oluşturulurken hata oluştu: {Code}", model.Code);
                return ServiceResult<CreateMachineViewModel>.Fail("Makine oluşturulurken beklenmeyen bir hata oluştu");
            }
        }

        /// <summary>
        /// Makine günceller
        /// </summary>
        public async Task<ServiceResult> UpdateAsync(UpdateMachineViewModel model)
        {
            try
            {
                var result = await _apiService.PutServiceResultAsync($"{API_ENDPOINT}/{model.Id}", model);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Makine güncellendi: {Id} - {Code}", model.Id, model.Code);
                    return ServiceResult.Success();
                }

                return ServiceResult.Fail(
                    result.ErrorMessage ?? new List<string> { "Makine güncellenemedi" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine güncellenirken hata oluştu. ID: {Id}", model.Id);
                return ServiceResult.Fail("Makine güncellenirken beklenmeyen bir hata oluştu");
            }
        }

        /// <summary>
        /// Makine siler (soft delete)
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(int id)
        {
            try
            {
                var result = await _apiService.DeleteServiceResultAsync($"{API_ENDPOINT}/{id}");

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Makine silindi: {Id}", id);
                    return ServiceResult.Success();
                }

                return ServiceResult.Fail(
                    result.ErrorMessage ?? new List<string> { "Makine silinemedi" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine silinirken hata oluştu. ID: {Id}", id);
                return ServiceResult.Fail("Makine silinirken beklenmeyen bir hata oluştu");
            }
        }

        /// <summary>
        /// Makine aktiflik durumunu değiştirir
        /// </summary>
        public async Task<ServiceResult> SetActiveStatusAsync(int id, bool isActive)
        {
            try
            {
                var result = await _apiService.PutServiceResultAsync($"{API_ENDPOINT}/{id}/status?isActive={isActive}", null);

                if (result.IsSuccess)
                {
                    var status = isActive ? "aktif" : "pasif";
                    _logger.LogInformation("Makine durumu güncellendi: {Id} - {Status}", id, status);
                    return ServiceResult.Success();
                }

                return ServiceResult.Fail(
                    result.ErrorMessage ?? new List<string> { "Makine durumu güncellenemedi" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine durumu güncellenirken hata oluştu. ID: {Id}", id);
                return ServiceResult.Fail("Makine durumu güncellenirken beklenmeyen bir hata oluştu");
            }
        }

        /// <summary>
        /// API bağlantısını test eder
        /// </summary>
        public async Task<ServiceResult<bool>> TestApiConnectionAsync(string apiAddress)
        {
            try
            {
                var result = await _apiService.GetServiceResultAsync<bool>($"{API_ENDPOINT}/test-connection?apiAddress={Uri.EscapeDataString(apiAddress)}");

                if (result.IsSuccess)
                {
                    var connectionStatus = result.Data ? "başarılı" : "başarısız";
                    _logger.LogInformation("API bağlantı testi {Status}: {ApiAddress}", connectionStatus, apiAddress);
                    return ServiceResult<bool>.Success(result.Data);
                }

                return ServiceResult<bool>.Fail(
                    result.ErrorMessage ?? new List<string> { "Bağlantı testi yapılamadı" }, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API bağlantı testi sırasında hata oluştu: {ApiAddress}", apiAddress);
                return ServiceResult<bool>.Fail("Bağlantı testi sırasında beklenmeyen bir hata oluştu");
            }
        }
    }
}
