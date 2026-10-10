using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShopFast.Checkout.Inventory;
using ShopFast.Checkout.Orders;
using ShopFast.Checkout.Payment;

namespace ShopFast.Checkout.Tests.Fixtures;

/// <summary>
/// Happy-path fakes for inventory-service and payment-gateway, plus helpers to
/// build the real clients on top of any <see cref="HttpMessageHandler"/>.
/// </summary>
public static class DownstreamFakes
{
    public static FakeHttpMessageHandler InventoryReservesEverything()
    {
        return new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.Created, new
        {
            reservation_id = $"rsv_{Guid.NewGuid():N}",
            status = "held",
            expires_at = DateTimeOffset.UtcNow.AddMinutes(15),
        }));
    }

    public static FakeHttpMessageHandler PaymentGatewayApprovesEverything()
    {
        return new FakeHttpMessageHandler(request =>
        {
            using var charge = JsonDocument.Parse(request.Body);
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, new
            {
                charge_id = $"ch_{Guid.NewGuid():N}",
                status = "succeeded",
                amount_minor = charge.RootElement.GetProperty("amount_minor").GetInt64(),
                currency = charge.RootElement.GetProperty("currency").GetString(),
            });
        });
    }

    public static InventoryClient CreateInventoryClient(
        HttpMessageHandler handler, InventoryServiceOptions? options = null)
    {
        return new InventoryClient(
            new HttpClient(handler),
            Options.Create(options ?? new InventoryServiceOptions()),
            NullLogger<InventoryClient>.Instance);
    }

    public static PaymentClient CreatePaymentClient(
        HttpMessageHandler handler, PaymentGatewayOptions? options = null)
    {
        return new PaymentClient(
            new HttpClient(handler),
            Options.Create(options ?? new PaymentGatewayOptions { ApiKey = "test-api-key" }),
            NullLogger<PaymentClient>.Instance);
    }

    public static OrderWriter CreateOrderWriter(CheckoutDbContext dbContext, TimeProvider? timeProvider = null)
    {
        return new OrderWriter(
            dbContext,
            timeProvider ?? TimeProvider.System,
            NullLogger<OrderWriter>.Instance);
    }
}
