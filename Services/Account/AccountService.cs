using App.Repositories.UserApps;
using App.Services.Account.Dtos;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace App.Services.Account
{
    public class AccountService(UserManager<UserApp> userManager) : IAccountService
    {
        public async Task<ServiceResult> PasswordChangeAsync(PasswordChangeDTO model)
        {
            var user = await userManager.FindByIdAsync(model.UserId);

            if (user is null) return ServiceResult.Fail("UserName not found", HttpStatusCode.NotFound);

            bool exist = userManager.CheckPasswordAsync(user, model.OldPassword).Result;

            if (exist)
            {
                var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

                if (!result.Succeeded) return ServiceResult.Fail("Parola Hatalı bilgilerinizi kontrol ediniz!");

                var updateSecurityStamp = userManager.UpdateSecurityStampAsync(user);
            }

            return ServiceResult.Success();
        }
    }
}
