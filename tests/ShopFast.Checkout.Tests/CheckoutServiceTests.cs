using Microsoft.EntityFrameworkCore;
using ShopFast.Checkout.Cart;
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

        var inventory = DownstreamFakes.InventoryReservesEverything();
        var paymentGateway = DownstreamFakes.PaymentGatewayApprovesEverything();

        var service = new CheckoutService(
            dbContext,
            new CartRepository(dbContext),
            DownstreamFakes.CreateInventoryClient(inventory),
            DownstreamFakes.CreatePaymentClient(paymentGateway),
            DownstreamFakes.CreateOrderWriter(dbContext));

        var result = await service.StartAsync(cart.Id);

        Assert.Equal(cart.Id, result.CartId);
        Assert.False(string.IsNullOrWhiteSpace(result.ReservationId));
        Assert.False(string.IsNullOrWhiteSpace(result.ChargeId));
        Assert.False(string.IsNullOrWhiteSpace(result.OrderNumber));

        Assert.Equal("/api/v2/reservations", Assert.Single(inventory.Requests).Uri.AbsolutePath);
        Assert.Equal("/v1/charge", Assert.Single(paymentGateway.Requests).Uri.AbsolutePath);

        var order = await dbContext.Orders
            .Include(o => o.Lines)
            .SingleAsync(o => o.OrderNumber == result.OrderNumber);
        Assert.Equal(result.ChargeId, order.ChargeId);
        Assert.Equal(6, order.Lines.Count);
        Assert.Equal(order.Lines.Sum(l => l.UnitPrice * l.Quantity), order.TotalAmount);
    }
}
