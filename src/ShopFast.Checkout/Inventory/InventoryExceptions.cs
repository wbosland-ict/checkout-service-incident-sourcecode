namespace ShopFast.Checkout.Inventory;

public class InventoryServiceException : Exception
{
    public int? StatusCode { get; }

    public InventoryServiceException(int? statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

public class InsufficientStockException : Exception
{
    public IReadOnlyList<string> UnavailableSkus { get; }

    public InsufficientStockException(IReadOnlyList<string> unavailableSkus)
        : base($"Insufficient stock for {unavailableSkus.Count} SKU(s): {string.Join(", ", unavailableSkus)}.")
    {
        UnavailableSkus = unavailableSkus;
    }
}
