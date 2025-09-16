using App.UI.Application.DTOS;
using App.UI.Infrastructure.Http;

namespace App.UI.Application.Services
{
    public interface IRoleService
    {
        Task<ServiceResult<List<RoleDto>>> GetAllRolesAsync();
        Task<ServiceResult<RoleDto>> GetRoleByIdAsync(string id);
        Task<ServiceResult> CreateRoleAsync(RoleDto roleDto);
        Task<ServiceResult> UpdateRoleAsync(RoleDto roleDto);
        Task<ServiceResult> DeleteRoleAsync(string id);
        Task<ServiceResult<List<RoleAssignDtoUI>>> GetUserRolesAsync(string userId);
        Task<ServiceResult> AssignRolesToUserAsync(List<RoleAssignDtoUI> roles, string userId);
        Task<ServiceResult<UserAppDtoUI>> GetUserByIdAsync(string userId);
    }

    public class RoleService(IApiService apiService) : IRoleService
    {
        public async Task<ServiceResult<List<RoleDto>>> GetAllRolesAsync()
        {
            try
            {
                var result = await apiService.GetServiceResultAsync<List<RoleDto>>("api/v1/Role");
                return result;
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RoleDto>>.Fail($"Roller yüklenirken hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult<RoleDto>> GetRoleByIdAsync(string id)
        {
            return await apiService.GetServiceResultAsync<RoleDto>($"api/v1/Role/{id}");
        }

        public async Task<ServiceResult> CreateRoleAsync(RoleDto roleDto)
        {
            return await apiService.PostServiceResultAsync("api/v1/Role", roleDto);
        }

        public async Task<ServiceResult> UpdateRoleAsync(RoleDto roleDto)
        {
            return await apiService.PutServiceResultAsync($"api/v1/Role/{roleDto.RoleId}", roleDto);
        }

        public async Task<ServiceResult> DeleteRoleAsync(string id)
        {
            var data = new { id };
            return await apiService.DeleteServiceResultAsync($"api/v1/Role/{id}", data);
        }

        public async Task<ServiceResult<List<RoleAssignDtoUI>>> GetUserRolesAsync(string userId)
        {
            try
            {
                var request = new GetUserRolesRequest(userId);
                var result = await apiService.PostServiceResultAsync<List<RoleAssignDtoUI>>("api/v1/Role/user-roles", request);
                return result;
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RoleAssignDtoUI>>.Fail($"Kullanıcı rolleri yüklenirken hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult> AssignRolesToUserAsync(List<RoleAssignDtoUI> roles, string userId)
        {
            return await apiService.PostServiceResultAsync($"api/v1/Role/assign/{userId}", roles);
        }

        public async Task<ServiceResult<UserAppDtoUI>> GetUserByIdAsync(string userId)
        {
            return await apiService.GetServiceResultAsync<UserAppDtoUI>($"api/v1/User/{userId}");
        }
    }
}
