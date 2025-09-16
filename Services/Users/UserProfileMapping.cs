using App.Repositories.UserApps;

using AutoMapper;

namespace App.Services.Users
{
    public class UserProfileMapping : Profile
    {
        public UserProfileMapping()
        {
            CreateMap<UserApp, UserAppDto>().ReverseMap();

            CreateMap<UserApp, UserAppDto>()
               .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
               .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
               .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.EmailConfirmed))
               .ForMember(dest => dest.Roles, opt => opt.Ignore())
               .ReverseMap();
        }
    }
}
