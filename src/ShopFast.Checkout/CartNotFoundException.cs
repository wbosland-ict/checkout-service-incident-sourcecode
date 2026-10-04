namespace ShopFast.Checkout;

public class CartNotFoundException : Exception
{
    public CartNotFoundException()
        : base("Cart not found.")
    {
    }
}
