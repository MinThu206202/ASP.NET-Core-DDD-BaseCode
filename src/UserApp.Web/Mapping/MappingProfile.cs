using AutoMapper;
using UserApp.Domain.Users;
using UserApp.Domain.Products;
using UserApp.Web.ViewModels;
using UserApp.Application.Users.DTOs;

namespace UserApp.Web.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ==================== USER MAPPINGS ====================
        // Read paths (Entity -> DTO/ViewModel)
        CreateMap<User, UserDto>();
        CreateMap<User, UserViewModel>();

        // Write/Update paths (DTO/ViewModel -> Entity)
        // Explicitly ignore 'Id' so AutoMapper doesn't overwrite your entity tracking identifier
        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
            
        CreateMap<UserViewModel, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());


        // ==================== PRODUCT MAPPINGS ====================
        // Read path (Entity -> ViewModel)
        CreateMap<Product, ProductViewModel>();

        // Write/Update path (ViewModel -> Entity)
        CreateMap<ProductViewModel, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}