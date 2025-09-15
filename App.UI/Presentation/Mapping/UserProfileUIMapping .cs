using App.Services.Users;
using App.UI.Application.DTOS;
using AutoMapper;

namespace App.UI.Presentation.Mapping
{
    public class UserProfileUIMapping : Profile
    {
        public UserProfileUIMapping()
        {
            CreateMap<UserAppDto, UserAppDtoUI>().ReverseMap();
        }
    }
}
