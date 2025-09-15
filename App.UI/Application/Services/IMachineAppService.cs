using App.UI.Infrastructure.Http;
using App.UI.Presentation.ViewModels;

namespace App.UI.Application.Services
{
    public interface IMachineAppService
    {
        Task<List<MachineListViewModel>> GetAllAsync();

        Task<MachineViewModel?> GetByIdAsync(int id);

        Task<CreateMachineViewModel?> CreateAsync(CreateMachineViewModel model);

        Task<bool> UpdateAsync(UpdateMachineViewModel model);

        Task<bool> DeleteAsync(int id);
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
        public async Task<List<MachineListViewModel>> GetAllAsync()
        {
            try
            {
                var machines = await _apiService.GetAsync<List<MachineListViewModel>>(API_ENDPOINT);
                return machines ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makineler yüklenirken hata oluştu");
                return [];
            }
        }

        /// <summary>
        /// ID'ye göre makine getirir
        /// </summary>
        public async Task<MachineViewModel?> GetByIdAsync(int id)
        {
            try
            {
                return await _apiService.GetAsync<MachineViewModel>($"{API_ENDPOINT}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine bilgisi alınırken hata oluştu. ID: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Yeni makine oluşturur
        /// </summary>
        public async Task<CreateMachineViewModel?> CreateAsync(CreateMachineViewModel model)
        {
            try
            {
                return await _apiService.PostAsync<CreateMachineViewModel>(API_ENDPOINT, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine oluşturulurken hata oluştu");
                return null;
            }
        }

        /// <summary>
        /// Makine günceller
        /// </summary>
        public async Task<bool> UpdateAsync(UpdateMachineViewModel model)
        {
            try
            {
                return await _apiService.PutAsync<bool>($"{API_ENDPOINT}/{model.Id}", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine güncellenirken hata oluştu. ID: {Id}", model.Id);
                return false;
            }
        }

        /// <summary>
        /// Makine siler
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                return await _apiService.DeleteAsync<bool>($"{API_ENDPOINT}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Makine silinirken hata oluştu. ID: {Id}", id);
                return false;
            }
        }
    }
}
