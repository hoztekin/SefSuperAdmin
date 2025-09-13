using App.Services.Machine.Dtos;
using App.Services.Machine.Validation;
using AutoMapper;

namespace App.Services.Machine
{
    public class MachineProfile : Profile
    {
        public MachineProfile()
        {
            // Entity to DTO mappings
            CreateMap<Repositories.Machines.Machine, MachineDto>();
            CreateMap<Repositories.Machines.Machine, MachineListDto>();

            // DTO to Entity mappings
            CreateMap<CreateMachineDto, Repositories.Machines.Machine>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            CreateMap<UpdateMachineDto, Repositories.Machines.Machine>()
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
        }
    }
}
