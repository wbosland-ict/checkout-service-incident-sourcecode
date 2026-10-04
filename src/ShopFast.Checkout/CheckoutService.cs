using ShopFast.Checkout.Cart;
using ShopFast.Checkout.Inventory;
using ShopFast.Checkout.Orders;
using ShopFast.Checkout.Payment;

namespace ShopFast.Checkout;

public class CheckoutService
{
    private readonly CheckoutDbContext _dbContext;
    private readonly ICartRepository _cartRepository;
    private readonly IInventoryClient _inventoryClient;
    private readonly IPaymentClient _paymentClient;
    private readonly IOrderWriter _orderWriter;

    public CheckoutService(
        CheckoutDbContext dbContext,
        ICartRepository cartRepository,
        IInventoryClient inventoryClient,
        IPaymentClient paymentClient,
        IOrderWriter orderWriter)
    {
        _dbContext = dbContext;
        _cartRepository = cartRepository;
        _inventoryClient = inventoryClient;
        _paymentClient = paymentClient;
        _orderWriter = orderWriter;
    }

    public async Task<CheckoutResult> StartAsync(long cartId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var cart = await _cartRepository.GetByIdAsync(cartId)
            ?? throw new CartNotFoundException();

        var lines = cart.Items
            .Select(i => new LineItem(i.Product.Sku, i.Product.Price, i.Quantity))
            .ToList();

        var reservation = await _inventoryClient.ReserveAsync(lines);
        var payment = await _paymentClient.ChargeAsync(reservation, lines);
        var result = await _orderWriter.CreateAsync(cartId, reservation, payment);

        await transaction.CommitAsync();
        return result;
    }
}
