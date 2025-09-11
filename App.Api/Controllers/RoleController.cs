using App.Services.Roles;
using App.Services.Roles.Create;
using App.Services.Roles.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{

    public class RoleController(IRoleService roleService) : CustomBaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetAllRoles() => CreateActionResult(await roleService.GetAllRolesAsync());

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] RoleRequest request) => CreateActionResult(await roleService.CreateRoleAsync(request));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRole(string id) => CreateActionResult(await roleService.DeleteRoleAsync(id));

        [HttpPost("user-roles")]
        public async Task<IActionResult> GetUserRoles([FromBody] GetUserRolesRequest request) => CreateActionResult(await roleService.GetUserRolesAsync(request));

        [HttpPost("assign/{userId}")]
        public async Task<IActionResult> AssignRoles([FromBody] List<RoleAssignDTO> request, string userId) => CreateActionResult(await roleService.RoleAssignAsync(request, userId));
    }
}
