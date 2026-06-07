namespace UserApp.Web.ViewModels.Payments;

public class PaymentViewModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}