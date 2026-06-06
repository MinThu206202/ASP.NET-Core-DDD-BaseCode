using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Users.Interfaces;
using UserApp.Application.Users.DTOs;
using UserApp.Domain.Users;

namespace UserApp.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // JWT required
    public class UsersApiController : BaseApiController<User, UserDto>
    {
        public UsersApiController(IUserService service, IMapper mapper) 
            : base(service, mapper)
        {
        }
    }
}