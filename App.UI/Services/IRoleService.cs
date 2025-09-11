using App.UI.DTOS;

namespace App.UI.Services
{
    public interface IRoleService
    {
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<RoleDto> GetRoleByIdAsync(string id);
        Task CreateRoleAsync(RoleDto roleDto);
        Task UpdateRoleAsync(RoleDto roleDto);
        Task DeleteRoleAsync(string id);
        Task<List<RoleAssignDtoUI>> GetUserRolesAsync(string userId);
        Task AssignRolesToUserAsync(List<RoleAssignDtoUI> roles, string userId);
        Task<UserAppDtoUI> GetUserByIdAsync(string userId);
    }

    public class RoleService(IApiService apiService) : IRoleService
    {
        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            try
            {
                var result = await apiService.GetAsync<List<RoleDto>>("api/v1/Role");
                return result ?? new List<RoleDto>();
            }
            catch (Exception ex)
            {
                return new List<RoleDto>();
            }
        }

        public async Task<RoleDto> GetRoleByIdAsync(string id)
        {
            return await apiService.GetAsync<RoleDto>($"api/v1/Role/{id}");
        }

        public async Task CreateRoleAsync(RoleDto roleDto)
        {
            var response = await apiService.PostAsync<ServiceResult<RoleDto>>("api/v1/Role", roleDto);
        }

        public async Task UpdateRoleAsync(RoleDto roleDto)
        {
            await apiService.PutAsync<RoleDto>($"api/v1/Role/{roleDto.RoleId}", roleDto);
        }

        public async Task DeleteRoleAsync(string id)
        {
            var data = new { id };
            await apiService.DeleteAsync<ServiceResult<object>>($"api/v1/Role/{id}", data);
        }

        public async Task<List<RoleAssignDtoUI>> GetUserRolesAsync(string userId)
        {
            try
            {
                var request = new GetUserRolesRequest(userId);
                var result = await apiService.PostAsync<List<RoleAssignDtoUI>>("api/v1/Role/user-roles", request);
                return result ?? new List<RoleAssignDtoUI>();
            }
            catch (Exception ex)
            {
                return new List<RoleAssignDtoUI>();
            }
        }

        public async Task AssignRolesToUserAsync(List<RoleAssignDtoUI> roles, string userId)
        {
            await apiService.PostAsync<List<RoleAssignDtoUI>>($"api/v1/Role/assign/{userId}", roles);
        }

        public async Task<UserAppDtoUI> GetUserByIdAsync(string userId)
        {
            return await apiService.GetAsync<UserAppDtoUI>($"api/v1/User/{userId}");
        }
    }
}
