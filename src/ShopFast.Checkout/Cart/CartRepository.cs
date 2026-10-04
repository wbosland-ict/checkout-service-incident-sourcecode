namespace ShopFast.Checkout.Cart;

public class CartRepository : ICartRepository
{
    private readonly CheckoutDbContext _dbContext;

    public CartRepository(CheckoutDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Cart?> GetByIdAsync(long cartId)
    {
        return await _dbContext.Carts.FindAsync(cartId);
    }
}
