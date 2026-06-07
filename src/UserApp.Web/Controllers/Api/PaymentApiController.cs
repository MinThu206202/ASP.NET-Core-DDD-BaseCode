using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Payments.Interfaces;
using UserApp.Domain.Payments;
using UserApp.Web.ViewModels.Payments;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentApiController : BaseApiController<Payment, PaymentViewModel>
{
    public PaymentApiController(IPaymentService service, IMapper mapper)
        : base(service, mapper)
    {
    }
}