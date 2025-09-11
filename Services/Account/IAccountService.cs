using App.Services.Account.Dtos;

namespace App.Services.Account
{
    public interface IAccountService
    {
        Task<ServiceResult> PasswordChangeAsync(PasswordChangeDTO model);
    }
}
