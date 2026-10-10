using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopFast.Checkout.Inventory;
using ShopFast.Checkout.Payment;

namespace ShopFast.Checkout.Orders;

public interface IOrderWriter
{
    Task<CheckoutResult> CreateAsync(long cartId, ReservationResult reservation, PaymentResult payment);
}

public class OrderWriter : IOrderWriter
{
    private readonly CheckoutDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OrderWriter> _logger;

    public OrderWriter(CheckoutDbContext dbContext, TimeProvider timeProvider, ILogger<OrderWriter> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CheckoutResult> CreateAsync(long cartId, ReservationResult reservation, PaymentResult payment)
    {
        var snapshot = await _dbContext.Carts
            .AsNoTracking()
            .Where(c => c.Id == cartId)
            .Select(c => new
            {
                c.CustomerId,
                Lines = c.Items
                    .Select(i => new OrderLine
                    {
                        Sku = i.Product.Sku,
                        ProductName = i.Product.Name,
                        UnitPrice = i.Product.Price,
                        Quantity = i.Quantity,
                    })
                    .ToList(),
            })
            .SingleOrDefaultAsync()
            ?? throw new CartNotFoundException();

        var orderTotal = snapshot.Lines.Sum(l => l.UnitPrice * l.Quantity);
        if (orderTotal != payment.Amount)
        {
            _logger.LogWarning(
                "order.amount_mismatch cart_id={CartId} order_total={OrderTotal} charged_amount={ChargedAmount} charge_id={ChargeId}",
                cartId, orderTotal, payment.Amount, payment.ChargeId);
        }

        var placedAt = _timeProvider.GetUtcNow();
        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(placedAt),
            CartId = cartId,
            CustomerId = snapshot.CustomerId,
            ReservationId = reservation.ReservationId,
            ChargeId = payment.ChargeId,
            TotalAmount = payment.Amount,
            Currency = payment.Currency,
            Status = OrderStatus.Placed,
            PlacedAt = placedAt,
            Lines = snapshot.Lines,
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "order.created order_number={OrderNumber} cart_id={CartId} lines={LineCount} total={Total} currency={Currency}",
            order.OrderNumber, cartId, order.Lines.Count, order.TotalAmount, order.Currency);

        return new CheckoutResult(cartId, reservation.ReservationId, payment.ChargeId, order.OrderNumber);
    }

    private static string GenerateOrderNumber(DateTimeOffset placedAt)
    {
        return $"SF-{placedAt:yyyyMMdd}-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
    }
}
