using Microsoft.EntityFrameworkCore;
using ShopFast.Checkout.Cart;

namespace ShopFast.Checkout;

public class CheckoutDbContext : DbContext
{
    public CheckoutDbContext(DbContextOptions<CheckoutDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cart.Cart> Carts => Set<Cart.Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart.Cart>(cart =>
        {
            cart.HasKey(c => c.Id);
            cart.HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(i => i.CartId);
        });

        modelBuilder.Entity<CartItem>(item =>
        {
            item.HasKey(i => i.Id);
            item.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId);
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.HasKey(p => p.Id);
        });
    }
}
