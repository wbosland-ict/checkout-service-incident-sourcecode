namespace ShopFast.Checkout.Cart;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(long cartId);
}
