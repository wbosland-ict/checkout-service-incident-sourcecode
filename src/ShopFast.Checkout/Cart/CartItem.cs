namespace ShopFast.Checkout.Cart;

public class CartItem
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public int Quantity { get; set; }

    public long ProductId { get; set; }

    public virtual Product Product { get; set; } = null!;
}
