using App.Services.Users;
using App.UI.DTOS;
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
