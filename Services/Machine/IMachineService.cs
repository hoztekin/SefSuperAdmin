using App.Services.Machine.Dtos;
using App.Services.Machine.Validation;

namespace App.Services.Machine
{
    public interface IMachineService
    {
        // CRUD Operations
        Task<ServiceResult<List<MachineListDto>>> GetAllListAsync();
        Task<ServiceResult<MachineDto>> GetByIdAsync(int id);
        Task<ServiceResult<CreateMachineDto>> CreateAsync(CreateMachineDto createMachineDto);
        Task<ServiceResult> UpdateAsync(UpdateMachineDto updateMachineDto);
        Task<ServiceResult> DeleteAsync(int id);

        // Business Operations
        Task<ServiceResult<List<MachineListDto>>> GetActiveListAsync();
        Task<ServiceResult<List<MachineListDto>>> GetByBranchIdAsync(string branchId);
        Task<ServiceResult<MachineDto>> GetByCodeAsync(string code);
        Task<ServiceResult> SetActiveStatusAsync(int id, bool isActive);
        Task<ServiceResult<bool>> CheckApiConnectionAsync(string apiAddress);

        // Query Operations
        Task<ServiceResult<bool>> IsCodeExistsAsync(string code, int? excludeId = null);
        Task<ServiceResult<bool>> IsBranchIdExistsAsync(string branchId, int? excludeId = null);
    }
}
