using ShopFast.Checkout.Cart;
using ShopFast.Checkout.Inventory;
using ShopFast.Checkout.Orders;
using ShopFast.Checkout.Payment;
using ShopFast.Checkout.Tests.Fixtures;

namespace ShopFast.Checkout.Tests;

public class CheckoutServiceTests
{
    [Fact]
    public async Task StartAsync_WithRealisticCart_CompletesCheckout()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateContext();
        await using var disposableConnection = connection;
        await using var disposableDbContext = dbContext;

        var cart = CartFixtures.CreateCartWithItems(dbContext, itemCount: 6);

        var service = new CheckoutService(
            dbContext,
            new CartRepository(dbContext),
            new InventoryClient(),
            new PaymentClient(),
            new OrderWriter());

        var result = await service.StartAsync(cart.Id);

        Assert.Equal(cart.Id, result.CartId);
        Assert.False(string.IsNullOrWhiteSpace(result.ReservationId));
        Assert.False(string.IsNullOrWhiteSpace(result.ChargeId));
    }
}
