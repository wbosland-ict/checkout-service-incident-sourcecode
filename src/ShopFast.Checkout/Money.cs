namespace ShopFast.Checkout;

internal static class Money
{
    public static long ToMinorUnits(decimal amount)
    {
        return (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal FromMinorUnits(long minorUnits)
    {
        return minorUnits / 100m;
    }
}
