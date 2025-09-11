using App.Services.Roles.Create;
using App.Services.Roles.DTOs;

namespace App.Services.Roles
{
    public interface IRoleService
    {
        Task<ServiceResult<IEnumerable<AllRolesDto>>> GetAllRolesAsync();
        Task<ServiceResult> CreateRoleAsync(RoleRequest request);
        Task<ServiceResult> DeleteRoleAsync(string id);
        Task<ServiceResult<List<RoleAssignDTO>>> GetUserRolesAsync(GetUserRolesRequest request);
        Task<ServiceResult> RoleAssignAsync(List<RoleAssignDTO> request, string userId);
    }
}
