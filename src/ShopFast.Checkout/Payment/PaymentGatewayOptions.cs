namespace ShopFast.Checkout.Payment;

public class PaymentGatewayOptions
{
    public const string SectionName = "PaymentGateway";

    public Uri BaseUrl { get; set; } = new("https://api.payment-gateway.example/v1/");
    public string ApiKey { get; set; } = string.Empty;
    public string MerchantAccount { get; set; } = "shopfast-nl";
    public string Currency { get; set; } = "EUR";
    public int TimeoutMs { get; set; } = 3000;
    public PaymentRetryOptions Retry { get; set; } = new();
}

public class PaymentRetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int BackoffDelayMs { get; set; } = 200;
}
