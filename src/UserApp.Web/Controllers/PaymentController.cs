using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Payments.Interfaces;
using UserApp.Domain.Payments;
using UserApp.Web.ViewModels.Payments;

namespace UserApp.Web.Controllers;

public class PaymentController : BaseController<Payment, PaymentViewModel>
{
    public PaymentController(IPaymentService service, IMapper mapper)
        : base(service, mapper)
    {
    }
}