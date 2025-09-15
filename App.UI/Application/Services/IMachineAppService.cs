using App.Services;
using App.UI.DTOS;

using App.UI.Presentation.ViewModels;

namespace App.UI.Application.Services
{
    public interface IMachineAppService
    {
        Task<ServiceResult<List<MachineListViewModel>>> GetMachineListAsync();
        Task<ServiceResult<MachineDetailViewModel>> GetMachineByIdAsync(int id);
        Task<ServiceResult> CreateMachineAsync(CreateMachineViewModel model);
        Task<ServiceResult> UpdateMachineAsync(UpdateMachineViewModel model);
        Task<ServiceResult> DeleteMachineAsync(int id);
        Task<ServiceResult<bool>> CheckApiConnectionAsync(string apiAddress);
    }
}
