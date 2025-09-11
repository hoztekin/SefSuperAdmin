using App.Repositories.UserApps;
using App.Repositories.UserRoles;
using App.Services.Roles.Create;
using App.Services.Roles.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System.Net;

namespace App.Services.Roles
{
    public class RoleService(RoleManager<UserRole> roleManager, UserManager<UserApp> userManager, IMapper mapper) : IRoleService
    {
        public async Task<ServiceResult> CreateRoleAsync(RoleRequest request)
        {
            try
            {
                var appRole = mapper.Map<UserRole>(request);
                var result = await roleManager.CreateAsync(appRole);

                if (result.Succeeded)
                {
                    return ServiceResult.Success(HttpStatusCode.Created);
                }

                Log.Error($"Rol oluşturma sırasında hata oluştu");
                return ServiceResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)), HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Rol oluşturma sırasında hata oluştu {ex.Message}");
                return ServiceResult.Fail(ex.Message, HttpStatusCode.InternalServerError);

            }
        }



        public async Task<ServiceResult> DeleteRoleAsync(string id)
        {
            try
            {
                var appRole = await roleManager.FindByIdAsync(id);

                if (appRole is null)
                {
                    return ServiceResult.Fail("Rol bulunamadı", HttpStatusCode.NotFound);
                }

                var result = await roleManager.DeleteAsync(appRole);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ServiceResult.Fail(errors, HttpStatusCode.BadRequest);
                }

                return ServiceResult.Success(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Rol silme sırasında hata oluştu {ex.Message}");
                return ServiceResult.Fail($"Rol silme işlemi sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }



        public async Task<ServiceResult<IEnumerable<AllRolesDto>>> GetAllRolesAsync()
        {
            try
            {
                var roles = roleManager.Roles.AsQueryable();

                if (!roles.Any())
                {
                    return ServiceResult<IEnumerable<AllRolesDto>>.Fail("Hiç rol bulunamadı", HttpStatusCode.NotFound);
                }

                var roleDtos = mapper.Map<IEnumerable<AllRolesDto>>(roles);

                return ServiceResult<IEnumerable<AllRolesDto>>.Success(roleDtos, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Rol listelenmesini sırasında hata oluştu {ex.Message}");
                return ServiceResult<IEnumerable<AllRolesDto>>.Fail(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<List<RoleAssignDTO>>> GetUserRolesAsync(GetUserRolesRequest request)
        {
            try
            {
                var user = await userManager.FindByIdAsync(request.UserId);
                if (user is null) return ServiceResult<List<RoleAssignDTO>>.Fail("User not found", HttpStatusCode.NotFound);

                var roles = roleManager.Roles;
                var userRoles = await userManager.GetRolesAsync(user);
                var userRolesList = userRoles.ToList();

                var roleAssignDTOs = new List<RoleAssignDTO>();

                foreach (var role in roles)
                {
                    var roleAssignDTO = new RoleAssignDTO
                    {
                        UserId = user.Id.ToString(),
                        RoleId = role.Id,
                        RoleName = role.Name,
                        Exist = userRolesList.Contains(role.Name)
                    };

                    roleAssignDTOs.Add(roleAssignDTO);
                }

                return ServiceResult<List<RoleAssignDTO>>.Success(roleAssignDTOs, HttpStatusCode.OK);

            }
            catch (Exception ex)
            {
                Log.Fatal($"Kullanıcı rolleri alınırken hata oluştu {ex.Message}");
                return ServiceResult<List<RoleAssignDTO>>.Fail($"Kullanıcı rolleri alınırken hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> RoleAssignAsync(List<RoleAssignDTO> request, string userId)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);

                if (user is null)
                    return ServiceResult.Fail("Kullanıcı bulunamadı", HttpStatusCode.NotFound);

                foreach (var role in request)
                {
                    if (role.Exist)
                    {
                        var addResult = await userManager.AddToRoleAsync(user, role.RoleName);
                        if (!addResult.Succeeded)
                        {
                            return ServiceResult.Fail($"'{role.RoleName}' rolü eklenirken hata oluştu: {string.Join(", ", addResult.Errors.Select(e => e.Description))}", HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        var removeResult = await userManager.RemoveFromRoleAsync(user, role.RoleName);

                        if (!removeResult.Succeeded)
                        {
                            return ServiceResult.Fail($"'{role.RoleName}' rolü kaldırılırken hata oluştu: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}", HttpStatusCode.BadRequest);
                        }
                    }
                }

                return ServiceResult.Success(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Rol atama işlemi sırasında hata oluştu {ex.Message}");
                return ServiceResult.Fail($"Rol atama işlemi sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
    }
}
