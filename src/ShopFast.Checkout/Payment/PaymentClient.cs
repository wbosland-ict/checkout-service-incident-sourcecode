using ShopFast.Checkout.Inventory;

namespace ShopFast.Checkout.Payment;

public record PaymentResult(string ChargeId);

public interface IPaymentClient
{
    Task<PaymentResult> ChargeAsync(ReservationResult reservation, IReadOnlyList<LineItem> lines);
}

public class PaymentClient : IPaymentClient
{
    public async Task<PaymentResult> ChargeAsync(ReservationResult reservation, IReadOnlyList<LineItem> lines)
    {
        await Task.Delay(1);
        return new PaymentResult(Guid.NewGuid().ToString("N"));
    }
}
