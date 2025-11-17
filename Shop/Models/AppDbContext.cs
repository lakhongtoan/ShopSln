using Microsoft.EntityFrameworkCore;

namespace Shop.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // --- SHOP DATA ---
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Slider> Sliders { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<CampingTip> CampingTips { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product - Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Brand (Brand KHÔNG có Products → WithMany())
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany()
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            // Order - OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem - Product
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // CartItem - Product
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product - ProductReview
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // INDEXES
            modelBuilder.Entity<Product>().HasIndex(p => p.CategoryId);
            modelBuilder.Entity<Product>().HasIndex(p => p.IsActive);
            modelBuilder.Entity<Product>().HasIndex(p => p.IsFeatured);

            modelBuilder.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderDate);

            modelBuilder.Entity<CartItem>().HasIndex(ci => ci.SessionId);

            modelBuilder.Entity<ProductReview>().HasIndex(pr => pr.ProductId);
            modelBuilder.Entity<ProductReview>().HasIndex(pr => pr.UserId);
        }
    }
}
