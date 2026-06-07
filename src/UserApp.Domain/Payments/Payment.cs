using UserApp.Domain.Common;

namespace UserApp.Domain.Payments;

public class Payment : Entity<Guid>
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string PaymentMethod { get; set; } = string.Empty;
    // e.g. Card, Cash, BankTransfer

    public string Status { get; set; } = "Pending";
    // Pending, Success, Failed

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}