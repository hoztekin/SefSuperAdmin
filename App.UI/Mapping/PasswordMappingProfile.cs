using App.Services.Account.Dtos;
using App.UI.ViewModels;
using AutoMapper;

namespace App.UI.Mapping
{
    public class PasswordMappingProfile : Profile
    {
        public PasswordMappingProfile()
        {
            CreateMap<PasswordChangeViewModel, PasswordChangeDTO>().ReverseMap();
        }
    }
}
