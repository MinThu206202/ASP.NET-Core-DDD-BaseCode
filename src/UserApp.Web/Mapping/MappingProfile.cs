using AutoMapper;
using UserApp.Domain.Users;
using UserApp.Web.ViewModels;
using UserApp.Application.Users.DTOs;
using UserApp.Domain.Paps;
using UserApp.Domain.Milks;


namespace UserApp.Web.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ==================== USER MAPPINGS ====================
        CreateMap<User, UserDto>();
        CreateMap<User, UserViewModel>();

        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<UserViewModel, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());


        // ================= AUTO MAPPINGS =================
        // <AUTO-MAPPINGS-START>



CreateMap<Pap, PapViewModel>();
CreateMap<PapViewModel, Pap>();
CreateMap<Milk, MilkViewModel>();
CreateMap<MilkViewModel, Milk>();
        // <AUTO-MAPPINGS-END>

    }
}
