using App.Services.Account.Dtos;
using App.UI.Presentation.ViewModels;
using AutoMapper;

namespace App.UI.Presentation.Mapping
{
    public class PasswordMappingProfile : Profile
    {
        public PasswordMappingProfile()
        {
            CreateMap<PasswordChangeViewModel, PasswordChangeDTO>().ReverseMap();
        }
    }
}
