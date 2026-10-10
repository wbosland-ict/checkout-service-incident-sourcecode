namespace ShopFast.Checkout.Http;

internal static class UriExtensions
{
    // Without a trailing slash, relative paths replace the last segment
    // ("…/v1" + "charge" => "…/charge"), so normalise configured base URLs.
    public static Uri WithTrailingSlash(this Uri uri)
    {
        return uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
    }
}
