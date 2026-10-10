namespace ShopFast.Checkout;

public record CheckoutResult(long CartId, string ReservationId, string ChargeId, string OrderNumber);
