using ShopFast.Checkout.Inventory;

namespace ShopFast.Checkout.Payment;

public record PaymentResult(string ChargeId);

public class PaymentGatewayException : Exception
{
    public int StatusCode { get; }

    public PaymentGatewayException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

public class PaymentGatewayTimeoutException : Exception
{
    public PaymentGatewayTimeoutException(string message)
        : base(message)
    {
    }
}

public interface IPaymentClient
{
    Task<PaymentResult> ChargeAsync(ReservationResult reservation, IReadOnlyList<LineItem> lines);
}

public class PaymentClient : IPaymentClient
{
    private const int MaxAttempts = 3;
    private const int BackoffDelayMs = 200;

    public async Task<PaymentResult> ChargeAsync(ReservationResult reservation, IReadOnlyList<LineItem> lines)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await SendChargeRequestAsync(reservation, lines);
            }
            catch (PaymentGatewayTimeoutException) when (attempt < MaxAttempts)
            {
                await Task.Delay(BackoffDelayMs);
            }
            catch (PaymentGatewayException ex) when (attempt < MaxAttempts && IsRetryable(ex.StatusCode))
            {
                await Task.Delay(BackoffDelayMs);
            }
        }

        throw new PaymentGatewayException(502, $"Payment gateway did not succeed after {MaxAttempts} attempts.");
    }

    private static bool IsRetryable(int statusCode)
    {
        return statusCode == 429 || statusCode >= 500;
    }

    private async Task<PaymentResult> SendChargeRequestAsync(ReservationResult reservation, IReadOnlyList<LineItem> lines)
    {
        await Task.Delay(1);
        return new PaymentResult(Guid.NewGuid().ToString("N"));
    }
}
