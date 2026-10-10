namespace ShopFast.Checkout.Orders;

public enum OrderStatus
{
    Placed,
    Fulfilled,
    Cancelled,
}

public class Order
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public long CartId { get; set; }
    public long CustomerId { get; set; }
    public string ReservationId { get; set; } = string.Empty;
    public string ChargeId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTimeOffset PlacedAt { get; set; }

    public virtual List<OrderLine> Lines { get; set; } = new();
}

public class OrderLine
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
