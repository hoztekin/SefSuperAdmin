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
            try
            {
                var user = await userManager.FindByIdAsync(model.UserId);

                if (user is null)
                    return ServiceResult.Fail("Kullanıcı bulunamadı", HttpStatusCode.NotFound);

                bool exist = await userManager.CheckPasswordAsync(user, model.OldPassword);

                if (!exist)
                {
                    return ServiceResult.Fail("Mevcut şifreniz hatalı, lütfen kontrol edin", HttpStatusCode.BadRequest);
                }

                var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ServiceResult.Fail($"Şifre değiştirilemedi: {string.Join(", ", errors)}", HttpStatusCode.BadRequest);
                }

                var updateSecurityStamp = await userManager.UpdateSecurityStampAsync(user);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Şifre değiştirme işlemi sırasında hata oluştu: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
    }
}
