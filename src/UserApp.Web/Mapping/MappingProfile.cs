using AutoMapper;
using UserApp.Domain.Users;
using UserApp.Web.ViewModels;
using UserApp.Application.Users.DTOs;
using UserApp.Domain.Paps;
using UserApp.Domain.Milks;
using UserApp.Domain.Ais;
using UserApp.Domain.Cocos;
using UserApp.Web.ViewModels.Roles;
using UserApp.Domain.Roles;
using UserApp.Web.ViewModels.Permissions;
using UserApp.Domain.Categorys;
using UserApp.Domain.Payments;
using UserApp.Domain.CommonTables;
using UserApp.Web.ViewModels.CommonTables;
using UserApp.Domain.Products;
using UserApp.Web.ViewModels.Products;
using UserApp.Domain.Humans;


namespace UserApp.Web.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ==================== USER MAPPINGS ====================
        CreateMap<User, UserDto>();
        CreateMap<User, UserViewModel>();

        CreateMap<Role, RoleViewModel>()
            .ReverseMap();

        CreateMap<Permission, PermissionViewModel>()
                    .ReverseMap();

        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<UserViewModel, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());


        // ================= AUTO MAPPINGS =================
        // <AUTO-MAPPINGS-START>

        CreateMap<CommonTable, CommonTableViewModel>();
        CreateMap<CommonTableViewModel, CommonTable>();
        CreateMap<Product, ProductViewModel>();
        CreateMap<ProductViewModel, Product>();
        CreateMap<Pap, PapViewModel>();
        CreateMap<PapViewModel, Pap>();
        CreateMap<Milk, MilkViewModel>();
        CreateMap<MilkViewModel, Milk>();
        CreateMap<Ai, AiViewModel>();
        CreateMap<AiViewModel, Ai>();
        CreateMap<Coco, CocoViewModel>();
        CreateMap<CocoViewModel, Coco>();
CreateMap<Category, CategoryViewModel>();
CreateMap<CategoryViewModel, Category>();
CreateMap<Payment, PaymentViewModel>();
CreateMap<PaymentViewModel, Payment>();
CreateMap<Human, HumanViewModel>();
CreateMap<HumanViewModel, Human>();
        // <AUTO-MAPPINGS-END>
        CreateMap<RoleViewModel, Role>();
        CreateMap<PermissionViewModel, Permission>();



    }
}
