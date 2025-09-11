using App.Repositories.UserRoles;
using App.Services.Roles.Create;
using App.Services.Roles.DTOs;
using AutoMapper;

namespace App.Services.Roles
{
    public class RoleMappingProfile : Profile
    {
        public RoleMappingProfile()
        {
            CreateMap<RoleRequest, UserRole>().ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.RoleName));

            CreateMap<UserRole, RoleRequest>().ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Name));

            CreateMap<UserRole, AllRolesDto>().ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.Id)).ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Name));
        }
    }
}
