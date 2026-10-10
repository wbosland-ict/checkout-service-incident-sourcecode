using System.Net;
using System.Text;
using System.Text.Json;

namespace ShopFast.Checkout.Tests.Fixtures;

public record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string Body);

/// <summary>
/// Stands in for a downstream HTTP service. Every request is recorded, and the
/// responder decides what comes back (status code, headers, body), so tests
/// can simulate successful, failing, or slow downstream responses.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<RecordedRequest, CancellationToken, Task<HttpResponseMessage>> _responder;
    private readonly List<RecordedRequest> _requests = new();

    public FakeHttpMessageHandler(Func<RecordedRequest, HttpResponseMessage> responder)
        : this((request, _) => Task.FromResult(responder(request)))
    {
    }

    public FakeHttpMessageHandler(Func<RecordedRequest, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return _requests.ToList();
            }
        }
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, object body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value), StringComparer.OrdinalIgnoreCase);
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var recorded = new RecordedRequest(request.Method, request.RequestUri!, headers, body);
        lock (_requests)
        {
            _requests.Add(recorded);
        }

        var response = await _responder(recorded, cancellationToken);
        response.RequestMessage = request;
        return response;
    }
}
