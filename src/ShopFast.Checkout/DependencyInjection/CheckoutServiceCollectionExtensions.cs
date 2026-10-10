using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShopFast.Checkout.Cart;
using ShopFast.Checkout.Inventory;
using ShopFast.Checkout.Orders;
using ShopFast.Checkout.Payment;

namespace ShopFast.Checkout.DependencyInjection;

public static class CheckoutServiceCollectionExtensions
{
    // The host registers CheckoutDbContext itself (Npgsql + lazy-loading
    // proxies in production, see the config repo).
    public static IServiceCollection AddCheckout(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InventoryServiceOptions>(configuration.GetSection(InventoryServiceOptions.SectionName));
        services.Configure<PaymentGatewayOptions>(configuration.GetSection(PaymentGatewayOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<IInventoryClient, InventoryClient>();
        services.AddHttpClient<IPaymentClient, PaymentClient>();

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderWriter, OrderWriter>();
        services.AddScoped<CheckoutService>();

        return services;
    }
}
