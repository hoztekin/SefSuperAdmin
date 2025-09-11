using App.Services.Account.Dtos;
using App.UI.DTOS;
using AutoMapper;

namespace App.UI.Mapping
{
    public class PaswordMappingProfile : Profile
    {
        public PaswordMappingProfile()
        {
            CreateMap<PasswordChangeViewModel, PasswordChangeDTO>().ReverseMap();
        }
    }
}
