using App.Services.Account.Dtos;
using App.UI.Infrastructure.Http;
using App.UI.Presentation.ViewModels;
using AutoMapper;

namespace App.UI.Application.Services
{
    public interface IAccountService
    {
        Task PasswordChangeAsync(PasswordChangeViewModel model);
    }

    public class AccountService(IApiService apiService, IMapper mapper) : IAccountService
    {
        public async Task PasswordChangeAsync(PasswordChangeViewModel model)
        {
            var dto = mapper.Map<PasswordChangeDTO>(model);

            await apiService.PutAsync<PasswordChangeDTO>("api/v1/Account", dto);
        }
    }

}
