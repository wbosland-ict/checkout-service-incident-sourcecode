using ShopFast.Checkout.Cart;

namespace ShopFast.Checkout.Tests.Fixtures;

public static class CartFixtures
{
    public static Cart.Cart CreateCartWithItems(CheckoutDbContext dbContext, int itemCount, long customerId = 1)
    {
        if (itemCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "A cart needs at least one item.");
        }

        var products = Enumerable.Range(1, itemCount)
            .Select(i => new Product
            {
                Sku = $"SKU-{i:D4}",
                Name = $"Test product {i}",
                Price = 9.99m + i,
            })
            .ToList();
        dbContext.Products.AddRange(products);
        dbContext.SaveChanges();

        var cart = new Cart.Cart { CustomerId = customerId };
        dbContext.Carts.Add(cart);
        dbContext.SaveChanges();

        foreach (var product in products)
        {
            dbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = 1,
            });
        }
        dbContext.SaveChanges();

        return cart;
    }
}
