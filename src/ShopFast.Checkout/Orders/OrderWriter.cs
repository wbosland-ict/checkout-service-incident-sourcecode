using ShopFast.Checkout.Inventory;
using ShopFast.Checkout.Payment;

namespace ShopFast.Checkout.Orders;

public interface IOrderWriter
{
    Task<CheckoutResult> CreateAsync(long cartId, ReservationResult reservation, PaymentResult payment);
}

public class OrderWriter : IOrderWriter
{
    public async Task<CheckoutResult> CreateAsync(long cartId, ReservationResult reservation, PaymentResult payment)
    {
        await Task.Delay(1);
        return new CheckoutResult(cartId, reservation.ReservationId, payment.ChargeId);
    }
}
