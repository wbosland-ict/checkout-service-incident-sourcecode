using Microsoft.EntityFrameworkCore;
using ShopFast.Checkout.Cart;
using ShopFast.Checkout.Orders;

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
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

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

        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.Id);
            order.HasIndex(o => o.OrderNumber).IsUnique();
            order.HasIndex(o => o.CartId);
            order.Property(o => o.OrderNumber).HasMaxLength(32);
            order.Property(o => o.ReservationId).HasMaxLength(64);
            order.Property(o => o.ChargeId).HasMaxLength(64);
            order.Property(o => o.Currency).HasMaxLength(3);
            order.Property(o => o.TotalAmount).HasPrecision(12, 2);
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(16);
            order.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.HasKey(l => l.Id);
            line.Property(l => l.Sku).HasMaxLength(32);
            line.Property(l => l.UnitPrice).HasPrecision(12, 2);
        });
    }
}
