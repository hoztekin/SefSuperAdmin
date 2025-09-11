using App.Services.Account;
using App.Services.Account.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{

    public class AccountController(IAccountService accountService) : CustomBaseController
    {
        [HttpPut()]
        public async Task<IActionResult> PasswordChange([FromBody] PasswordChangeDTO request) => CreateActionResult(await accountService.PasswordChangeAsync(request));
    }
}
