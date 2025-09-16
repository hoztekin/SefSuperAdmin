using App.Services;
using App.Services.Account.Dtos;
using App.UI.Infrastructure.Http;
using App.UI.Presentation.ViewModels;
using AutoMapper;

namespace App.UI.Application.Services
{
    public interface IAccountService
    {
        Task<ServiceResult> PasswordChangeAsync(PasswordChangeViewModel model);
    }


    public class AccountService(IApiService apiService, IMapper mapper) : IAccountService
    {
        public async Task<ServiceResult> PasswordChangeAsync(PasswordChangeViewModel model)
        {
            var dto = mapper.Map<PasswordChangeDTO>(model);

            var result = await apiService.PutAsync<ServiceResult>("api/v1/Account", dto);

            return result;
        }
    }
}


