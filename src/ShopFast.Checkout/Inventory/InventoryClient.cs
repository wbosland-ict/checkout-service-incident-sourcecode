using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFast.Checkout.Http;

namespace ShopFast.Checkout.Inventory;

public record ReservationResult(string ReservationId, DateTimeOffset ExpiresAt);

public interface IInventoryClient
{
    Task<ReservationResult> ReserveAsync(IReadOnlyList<LineItem> lines);
}

public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly InventoryServiceOptions _options;
    private readonly ILogger<InventoryClient> _logger;

    public InventoryClient(
        HttpClient httpClient,
        IOptions<InventoryServiceOptions> options,
        ILogger<InventoryClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = _options.BaseUrl.WithTrailingSlash();
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    public async Task<ReservationResult> ReserveAsync(IReadOnlyList<LineItem> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("Cannot reserve stock for an empty cart.", nameof(lines));
        }

        var request = new ReservationRequest(
            lines
                .GroupBy(l => l.Sku)
                .Select(g => new ReservationRequestLine(g.Key, g.Sum(l => l.Quantity)))
                .ToList(),
            _options.ReservationTtlSeconds);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.TimeoutMs));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "reservations", request, DownstreamJson.Options, timeoutCts.Token);

            var status = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ReservationResponse>(
                    DownstreamJson.Options, timeoutCts.Token)
                    ?? throw new InventoryServiceException(status, "Inventory service returned an empty reservation.");

                _logger.LogDebug(
                    "inventory.call downstream=inventory-service status={StatusCode} skus={SkuCount} reservation_id={ReservationId} duration_ms={DurationMs}",
                    status, request.Lines.Count, body.ReservationId, stopwatch.ElapsedMilliseconds);

                return new ReservationResult(body.ReservationId, body.ExpiresAt);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var conflict = await response.Content.ReadFromJsonAsync<InsufficientStockResponse>(
                    DownstreamJson.Options, timeoutCts.Token);
                var unavailable = conflict?.UnavailableSkus ?? Array.Empty<string>();

                _logger.LogWarning(
                    "inventory.call downstream=inventory-service status={StatusCode} msg=\"insufficient stock\" unavailable_skus={UnavailableSkus} duration_ms={DurationMs}",
                    status, string.Join(',', unavailable), stopwatch.ElapsedMilliseconds);

                throw new InsufficientStockException(unavailable);
            }

            _logger.LogError(
                "inventory.call downstream=inventory-service status={StatusCode} msg=\"{Reason}\" duration_ms={DurationMs}",
                status, response.ReasonPhrase, stopwatch.ElapsedMilliseconds);

            throw new InventoryServiceException(status, $"Inventory reservation failed with HTTP {status}.");
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogError(
                "inventory.call downstream=inventory-service status=timeout timeout_ms={TimeoutMs} duration_ms={DurationMs}",
                _options.TimeoutMs, stopwatch.ElapsedMilliseconds);

            throw new InventoryServiceException(
                null, $"No response from inventory service within {_options.TimeoutMs} ms.", ex);
        }
    }

    private sealed record ReservationRequest(IReadOnlyList<ReservationRequestLine> Lines, int TtlSeconds);

    private sealed record ReservationRequestLine(string Sku, int Quantity);

    private sealed record ReservationResponse(string ReservationId, string Status, DateTimeOffset ExpiresAt);

    private sealed record InsufficientStockResponse(string Error, string[] UnavailableSkus);
}
