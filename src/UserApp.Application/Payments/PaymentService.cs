using UserApp.Domain.Payments;
using UserApp.Application.Common;
using UserApp.Application.Payments.Interfaces;

namespace UserApp.Application.Payments;

public class PaymentService : BaseService<Payment>, IPaymentService
{
    public PaymentService(IPaymentRepository repo) : base(repo)
    {
    }
}