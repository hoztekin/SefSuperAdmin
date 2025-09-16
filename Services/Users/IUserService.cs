using App.Services.Users.Create;
using App.Services.Users.Update;

namespace App.Services.Users
{
    public interface IUserService
    {
        Task<ServiceResult<UserAppDto>> CreateUserAsync(CreateUserDto createUserDto);
        Task<ServiceResult<UserAppDto>> GetUserByNameAsync(string userName);
        Task<ServiceResult<UserAppDto>> GetUserByUserIdAsync(string userId);
        Task<ServiceResult> CreateUserRoles(string usernName);
        Task<ServiceResult<List<UserAppDto>>> GetAllUsersAsync();
        Task<ServiceResult<UserAppDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);
        Task<ServiceResult> DeleteUserAsync(string userId);
    }
}
