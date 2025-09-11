using App.Services.Users;
using App.Services.Users.Create;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{

    public class UserController(IUserService userService) : CustomBaseController
    {
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto createUserDto) => CreateActionResult(await userService.CreateUserAsync(createUserDto));

        [HttpGet]
        public async Task<IActionResult> GetUser() => CreateActionResult(await userService.GetUserByNameAsync(HttpContext.User.Identity.Name));

        [HttpGet("AllMembers")]
        public async Task<IActionResult> GetAllUser() => CreateActionResult(await userService.GetAllUsersAsync());


        [HttpPost("CreateUserRoles/{userName}")]
        public async Task<IActionResult> CreateUserRoles(string userName) => CreateActionResult(await userService.CreateUserRoles(userName));

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId) => CreateActionResult(await userService.GetUserByUserIdAsync(userId));

    }
}
