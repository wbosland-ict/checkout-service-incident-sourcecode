namespace ShopFast.Checkout.Inventory;

public class InventoryServiceOptions
{
    public const string SectionName = "InventoryService";

    public Uri BaseUrl { get; set; } = new("http://inventory-service.fulfilment.svc.cluster.local/api/v2/");
    public int TimeoutMs { get; set; } = 2000;
    public int ReservationTtlSeconds { get; set; } = 900;
}
