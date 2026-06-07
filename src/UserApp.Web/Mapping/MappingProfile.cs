using AutoMapper;
using UserApp.Domain.Users;
using UserApp.Domain.Products;
using UserApp.Web.ViewModels;
using UserApp.Application.Users.DTOs;
using UserApp.Domain.Payments;
using UserApp.Web.ViewModels.Payments;


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

        // ==================== PRODUCT MAPPINGS ====================
        CreateMap<Product, ProductViewModel>();

        CreateMap<ProductViewModel, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // ==================== AUTO GENERATED MAPPINGS ====================
        // <AUTO-MAPPINGS-START>

        CreateMap<Payment, PaymentViewModel>();


        // <AUTO-MAPPINGS-END>
    }
}
