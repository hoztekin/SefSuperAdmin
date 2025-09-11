using App.Repositories.UserApps;

using AutoMapper;

namespace App.Services.Users
{
    public class UserProfileMapping : Profile
    {
        public UserProfileMapping()
        {
            CreateMap<UserApp, UserAppDto>().ReverseMap();
        }
    }
}
