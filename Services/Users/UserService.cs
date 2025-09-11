using App.Repositories.UserApps;
using App.Repositories.UserRoles;
using App.Services.Users.Create;
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
            var users = await userManager.Users.ToListAsync();
            var userAsDtoList = mapper.Map<List<UserAppDto>>(users);
            return ServiceResult<List<UserAppDto>>.Success(userAsDtoList, HttpStatusCode.OK);
        }

        public async Task<ServiceResult<UserAppDto>> GetUserByNameAsync(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);

            if (user == null)
            {
                return ServiceResult<UserAppDto>.Fail("UserName not found", HttpStatusCode.NotFound);
            }
            var userAsDto = mapper.Map<UserAppDto>(user);

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

            return ServiceResult<UserAppDto>.Success(userAsDto, HttpStatusCode.OK);
        }



    }
}
