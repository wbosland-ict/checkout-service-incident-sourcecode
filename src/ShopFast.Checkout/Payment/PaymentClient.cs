using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFast.Checkout.Http;
using ShopFast.Checkout.Inventory;

namespace ShopFast.Checkout.Payment;

public record PaymentResult(string ChargeId, decimal Amount, string Currency);

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

public class PaymentDeclinedException : Exception
{
    public string DeclineCode { get; }

    public PaymentDeclinedException(string declineCode, string message)
        : base(message)
    {
        DeclineCode = declineCode;
    }
}

public interface IPaymentClient
{
    Task<PaymentResult> ChargeAsync(ReservationResult reservation, IReadOnlyList<LineItem> lines);
}

public class PaymentClient : IPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly PaymentGatewayOptions _options;
    private readonly ILogger<PaymentClient> _logger;

    public PaymentClient(
        HttpClient httpClient,
        IOptions<PaymentGatewayOptions> options,
        ILogger<PaymentClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = _options.BaseUrl.WithTrailingSlash();
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<PaymentResult> ChargeAsync(ReservationResult reservation, IReadOnlyList<LineItem> lines)
    {
        var maxAttempts = _options.Retry.MaxAttempts;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await SendChargeRequestAsync(reservation, lines, attempt);
            }
            catch (PaymentGatewayTimeoutException) when (attempt < maxAttempts)
            {
                await DelayBeforeRetryAsync(attempt + 1);
            }
            catch (PaymentGatewayException ex) when (attempt < maxAttempts && IsRetryable(ex.StatusCode))
            {
                await DelayBeforeRetryAsync(attempt + 1);
            }
        }

        throw new PaymentGatewayException(502, $"Payment gateway did not succeed after {maxAttempts} attempts.");
    }

    private static bool IsRetryable(int statusCode)
    {
        return statusCode == 429 || statusCode >= 500;
    }

    private async Task DelayBeforeRetryAsync(int nextAttempt)
    {
        var delayMs = _options.Retry.BackoffDelayMs;
        _logger.LogInformation("retry.scheduled attempt={Attempt} delay_ms={DelayMs}", nextAttempt, delayMs);
        await Task.Delay(delayMs);
    }

    private async Task<PaymentResult> SendChargeRequestAsync(
        ReservationResult reservation, IReadOnlyList<LineItem> lines, int attempt)
    {
        var request = new ChargeRequest(
            AmountMinor: lines.Sum(l => Money.ToMinorUnits(l.Price) * l.Quantity),
            Currency: _options.Currency,
            MerchantAccount: _options.MerchantAccount,
            Reference: reservation.ReservationId,
            Description: $"ShopFast order ({lines.Sum(l => l.Quantity)} items)",
            LineItems: lines
                .Select(l => new ChargeRequestLine(l.Sku, l.Quantity, Money.ToMinorUnits(l.Price)))
                .ToList());

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.TimeoutMs));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "charge", request, DownstreamJson.Options, timeoutCts.Token);

            var status = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ChargeResponse>(
                    DownstreamJson.Options, timeoutCts.Token)
                    ?? throw new PaymentGatewayException(502, "Payment gateway returned an empty charge response.");

                _logger.LogInformation(
                    "payment.call downstream=payment-gateway status={StatusCode} attempt={Attempt} duration_ms={DurationMs}",
                    status, attempt, stopwatch.ElapsedMilliseconds);

                return new PaymentResult(body.ChargeId, Money.FromMinorUnits(body.AmountMinor), body.Currency);
            }

            var error = await TryReadErrorAsync(response, timeoutCts.Token);

            _logger.LogError(
                "payment.call downstream=payment-gateway status={StatusCode} attempt={Attempt} msg=\"{Reason}\"",
                status, attempt, attempt < _options.Retry.MaxAttempts ? response.ReasonPhrase : $"{response.ReasonPhrase}, giving up after {attempt} attempts");

            if (response.StatusCode == HttpStatusCode.PaymentRequired)
            {
                throw new PaymentDeclinedException(
                    error?.DeclineCode ?? "unknown",
                    error?.Message ?? "The payment was declined.");
            }

            throw new PaymentGatewayException(
                status, error?.Message ?? $"Payment gateway returned HTTP {status} ({response.ReasonPhrase}).");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogError(
                "payment.call downstream=payment-gateway status=timeout attempt={Attempt} timeout_ms={TimeoutMs} duration_ms={DurationMs}",
                attempt, _options.TimeoutMs, stopwatch.ElapsedMilliseconds);

            throw new PaymentGatewayTimeoutException(
                $"No response from payment gateway within {_options.TimeoutMs} ms.");
        }
    }

    private static async Task<GatewayError?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            return null;
        }

        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<GatewayErrorEnvelope>(
                DownstreamJson.Options, cancellationToken);
            return envelope?.Error;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed record ChargeRequest(
        long AmountMinor,
        string Currency,
        string MerchantAccount,
        string Reference,
        string Description,
        IReadOnlyList<ChargeRequestLine> LineItems);

    private sealed record ChargeRequestLine(string Sku, int Quantity, long UnitAmountMinor);

    private sealed record ChargeResponse(string ChargeId, string Status, long AmountMinor, string Currency);

    private sealed record GatewayErrorEnvelope(GatewayError? Error);

    private sealed record GatewayError(string? Type, string? Message, string? DeclineCode);
}
