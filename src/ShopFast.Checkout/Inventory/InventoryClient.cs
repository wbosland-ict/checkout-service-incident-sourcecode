namespace ShopFast.Checkout.Inventory;

public record ReservationResult(string ReservationId);

public interface IInventoryClient
{
    Task<ReservationResult> ReserveAsync(IReadOnlyList<LineItem> lines);
}

public class InventoryClient : IInventoryClient
{
    public async Task<ReservationResult> ReserveAsync(IReadOnlyList<LineItem> lines)
    {
        await Task.Delay(1);
        return new ReservationResult(Guid.NewGuid().ToString("N"));
    }
}
