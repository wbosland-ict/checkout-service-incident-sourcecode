namespace ShopFast.Checkout.Cart;

public class Cart
{
    public long Id { get; set; }
    public long CustomerId { get; set; }

    public virtual List<CartItem> Items { get; set; } = new();
}
