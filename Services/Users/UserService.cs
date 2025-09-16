using App.Repositories.UserApps;
using App.Repositories.UserRoles;
using App.Services.Users.Create;
using App.Services.Users.Update;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace App.Services.Users
{
    public class UserService(UserManager<UserApp> userManager, RoleManager<UserRole> roleManager, IMapper mapper) : IUserService
    {
        public async Task<ServiceResult<UserAppDto>> CreateUserAsync(CreateUserDto createUserDto)
        {
            var user = new UserApp { Email = createUserDto.EMail, UserName = createUserDto.UserName };

            var result = await userManager.CreateAsync(user, createUserDto.Password);

            if (!result.Succeeded)
            {
                var error = result.Errors.Select(x => x.Description).ToList();

                return ServiceResult<UserAppDto>.Fail("", HttpStatusCode.NotFound);
            }
            var userAsDto = mapper.Map<UserAppDto>(user);

            return ServiceResult<UserAppDto>.Success(userAsDto, HttpStatusCode.OK);
        }

        public async Task<ServiceResult> CreateUserRoles(string usernName)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new() { Name = "Admin" });
                await roleManager.CreateAsync(new() { Name = "Manager" });
            }


            var user = await userManager.FindByNameAsync(usernName);

            await userManager.AddToRoleAsync(user, "Admin");
            await userManager.AddToRoleAsync(user, "Manager");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<List<UserAppDto>>> GetAllUsersAsync()
        {
            try
            {
                var users = await userManager.Users.ToListAsync();
                var userAsDtoList = new List<UserAppDto>();

                foreach (var user in users)
                {
                    var userDto = mapper.Map<UserAppDto>(user);
                    var userRoles = await userManager.GetRolesAsync(user);
                    userDto.Roles = userRoles.ToList();

                    userAsDtoList.Add(userDto);
                }

                return ServiceResult<List<UserAppDto>>.Success(userAsDtoList, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserAppDto>>.Fail($"Kullanıcılar getirilemedi: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<UserAppDto>> GetUserByNameAsync(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);

            if (user == null)
            {
                return ServiceResult<UserAppDto>.Fail("UserName not found", HttpStatusCode.NotFound);
            }

            var userAsDto = mapper.Map<UserAppDto>(user);
            var userRoles = await userManager.GetRolesAsync(user);
            userAsDto.Roles = userRoles.ToList();

            return ServiceResult<UserAppDto>.Success(userAsDto, HttpStatusCode.OK);
        }

        public async Task<ServiceResult<UserAppDto>> GetUserByUserIdAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return ServiceResult<UserAppDto>.Fail("UserId not found", HttpStatusCode.NotFound);
            }

            var userAsDto = mapper.Map<UserAppDto>(user);
            var userRoles = await userManager.GetRolesAsync(user);
            userAsDto.Roles = userRoles.ToList();

            return ServiceResult<UserAppDto>.Success(userAsDto, HttpStatusCode.OK);
        }

        public async Task<ServiceResult<UserAppDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<UserAppDto>.Fail("Kullanıcı bulunamadı", HttpStatusCode.NotFound);
                }

                // Kullanıcı bilgilerini güncelle
                user.UserName = updateUserDto.UserName;
                user.Email = updateUserDto.EMail;

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(x => x.Description).ToList();
                    return ServiceResult<UserAppDto>.Fail(string.Join(", ", errors), HttpStatusCode.BadRequest);
                }

                // Şifre güncellenmesi varsa
                if (!string.IsNullOrEmpty(updateUserDto.Password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await userManager.ResetPasswordAsync(user, token, updateUserDto.Password);

                    if (!passwordResult.Succeeded)
                    {
                        var passwordErrors = passwordResult.Errors.Select(x => x.Description).ToList();
                        return ServiceResult<UserAppDto>.Fail($"Şifre güncelleme hatası: {string.Join(", ", passwordErrors)}", HttpStatusCode.BadRequest);
                    }
                }

                var userAsDto = mapper.Map<UserAppDto>(user);
                var userRoles = await userManager.GetRolesAsync(user);
                userAsDto.Roles = userRoles.ToList();

                return ServiceResult<UserAppDto>.Success(userAsDto, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserAppDto>.Fail($"Kullanıcı güncelleme hatası: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult.Fail("Kullanıcı bulunamadı", HttpStatusCode.NotFound);
                }

                // Kullanıcının rollerini temizle
                var userRoles = await userManager.GetRolesAsync(user);
                if (userRoles.Any())
                {
                    await userManager.RemoveFromRolesAsync(user, userRoles);
                }

                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(x => x.Description).ToList();
                    return ServiceResult.Fail(string.Join(", ", errors), HttpStatusCode.BadRequest);
                }

                return ServiceResult.Success(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Kullanıcı silme hatası: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

    }
}
