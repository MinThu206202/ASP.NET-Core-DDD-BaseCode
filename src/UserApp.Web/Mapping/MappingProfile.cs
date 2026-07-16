using AutoMapper;
using UserApp.Domain.Users;
using UserApp.Web.ViewModels;
using UserApp.Application.Users.DTOs;
using UserApp.Web.ViewModels.Roles;
using UserApp.Domain.Roles;
using UserApp.Web.ViewModels.Permissions;
using UserApp.Domain.CommonTables;
using UserApp.Web.ViewModels.CommonTables;
using UserApp.Domain.AuditLogs;
using UserApp.Web.ViewModels.AuditLogs;
using UserApp.Domain.Notifications;
using UserApp.Application.Notifications.DTOs;





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
        CreateMap<AuditLog, AuditLogViewModel>();
        CreateMap<AuditLogArchive, AuditLogViewModel>();



        CreateMap<Notification, NotificationDto>();
        // <AUTO-MAPPINGS-END>
        CreateMap<RoleViewModel, Role>();
        CreateMap<PermissionViewModel, Permission>();



    }
}
