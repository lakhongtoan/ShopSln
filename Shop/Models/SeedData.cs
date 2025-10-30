using Microsoft.EntityFrameworkCore;

namespace Shop.Models
{

    public static class SeedData
    {
        public static void EnsurePopulated(IApplicationBuilder app)
        {
            AppDbContext context = app.ApplicationServices
                .CreateScope().ServiceProvider
                .GetRequiredService<AppDbContext>();

            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

            // Seed Categories first
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Electronics", Description = "Electronic devices and gadgets", IsActive = true },
                    new Category { Name = "Clothing", Description = "Fashion and apparel", IsActive = true },
                    new Category { Name = "Home & Garden", Description = "Home improvement and garden supplies", IsActive = true },
                    new Category { Name = "Sports", Description = "Sports and fitness equipment", IsActive = true },
                    new Category { Name = "Books", Description = "Books and educational materials", IsActive = true }
                );
                context.SaveChanges();
            }

            // Seed Products
            if (!context.Products.Any())
            {
                var electronicsCategory = context.Categories.First(c => c.Name == "Electronics");
                var clothingCategory = context.Categories.First(c => c.Name == "Clothing");
                var sportsCategory = context.Categories.First(c => c.Name == "Sports");

                context.Products.AddRange(
                    new Product
                    {
                        Name = "Smartphone Pro Max",
                        Description = "Latest smartphone with advanced features",
                        CategoryId = electronicsCategory.CategoryId,
                        Price = 999.99m,
                        SalePrice = 899.99m,
                        StockQuantity = 50,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Wireless Headphones",
                        Description = "High-quality wireless headphones with noise cancellation",
                        CategoryId = electronicsCategory.CategoryId,
                        Price = 299.99m,
                        StockQuantity = 30,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Designer T-Shirt",
                        Description = "Premium cotton t-shirt with modern design",
                        CategoryId = clothingCategory.CategoryId,
                        Price = 49.99m,
                        StockQuantity = 100,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Running Shoes",
                        Description = "Comfortable running shoes for all terrains",
                        CategoryId = sportsCategory.CategoryId,
                        Price = 129.99m,
                        StockQuantity = 75,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Laptop Ultra",
                        Description = "High-performance laptop for professionals",
                        CategoryId = electronicsCategory.CategoryId,
                        Price = 1999.99m,
                        StockQuantity = 20,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Yoga Mat",
                        Description = "Non-slip yoga mat for home workouts",
                        CategoryId = sportsCategory.CategoryId,
                        Price = 39.99m,
                        StockQuantity = 60,
                        Image = "/images/home/anh.jpg"
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
