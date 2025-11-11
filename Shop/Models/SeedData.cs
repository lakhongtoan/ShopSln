using Microsoft.EntityFrameworkCore;

namespace Shop.Models
{
    public static class SeedData
    {
        public static void EnsurePopulated(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (context.Database.GetPendingMigrations().Any())
                context.Database.Migrate();

            var now = DateTime.Now;
            var defaultUser = "System";

            // ========================= SEED CATEGORIES =========================
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Electronics", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Clothing", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Home & Garden", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Sports", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Books", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser }
                );
                context.SaveChanges();
            }

            // ========================= SEED PRODUCTS =========================
            if (!context.Products.Any())
            {
                var electronics = context.Categories.First(c => c.Name == "Electronics");
                var clothing = context.Categories.First(c => c.Name == "Clothing");
                var sports = context.Categories.First(c => c.Name == "Sports");

                context.Products.AddRange(
                    new Product
                    {
                        Name = "Smartphone Pro Max",
                        Description = "Latest smartphone with advanced features",
                        CategoryId = electronics.CategoryId,
                        Price = 999.99m,
                        SalePrice = 899.99m,
                        StockQuantity = 50,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Wireless Headphones",
                        Description = "High-quality wireless headphones with noise cancellation",
                        CategoryId = electronics.CategoryId,
                        Price = 299.99m,
                        StockQuantity = 30,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Designer T-Shirt",
                        Description = "Premium cotton t-shirt with modern design",
                        CategoryId = clothing.CategoryId,
                        Price = 49.99m,
                        StockQuantity = 100,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Running Shoes",
                        Description = "Comfortable running shoes for all terrains",
                        CategoryId = sports.CategoryId,
                        Price = 129.99m,
                        StockQuantity = 75,
                        IsFeatured = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Laptop Ultra",
                        Description = "High-performance laptop for professionals",
                        CategoryId = electronics.CategoryId,
                        Price = 1999.99m,
                        StockQuantity = 20,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Yoga Mat",
                        Description = "Non-slip yoga mat for home workouts",
                        CategoryId = sports.CategoryId,
                        Price = 39.99m,
                        StockQuantity = 60,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    }
                );
                context.SaveChanges();
            }

            // ========================= SEED ORDERS =========================
            if (!context.Orders.Any())
            {
                var orders = new List<Order>
                {
                    new Order
                    {
                        OrderNumber = "ORD20251111001",
                        OrderDate = DateTime.Now.AddDays(-3),
                        Status = "Delivered",
                        SubTotal = 1500000,
                        TaxAmount = 150000,
                        ShippingAmount = 30000,
                        TotalAmount = 1680000,
                        CustomerName = "Nguyễn Văn A",
                        CustomerPhone = "0901234567",
                        CustomerEmail = "a@example.com",
                        ShippingAddress = "123 Nguyễn Huệ, Quận 1, TP.HCM",
                        Notes = "Giao giờ hành chính",
                        ShippedDate = DateTime.Now.AddDays(-2),
                        DeliveredDate = DateTime.Now.AddDays(-1)
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111002",
                        OrderDate = DateTime.Now.AddDays(-1),
                        Status = "Processing",
                        SubTotal = 850000,
                        TaxAmount = 85000,
                        ShippingAmount = 20000,
                        TotalAmount = 955000,
                        CustomerName = "Trần Thị B",
                        CustomerPhone = "0912345678",
                        CustomerEmail = "b@example.com",
                        ShippingAddress = "45 Lê Lợi, Quận 3, TP.HCM",
                        Notes = "Liên hệ trước khi giao",
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111003",
                        OrderDate = DateTime.Now,
                        Status = "Pending",
                        SubTotal = 450000,
                        TaxAmount = 45000,
                        ShippingAmount = 15000,
                        TotalAmount = 510000,
                        CustomerName = "Lê Văn C",
                        CustomerPhone = "0933456789",
                        CustomerEmail = "c@example.com",
                        ShippingAddress = "78 Hai Bà Trưng, Quận 1, TP.HCM"
                    }
                };

                context.Orders.AddRange(orders);
                context.SaveChanges();

                // ========================= SEED ORDER ITEMS =========================
                var orderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        OrderId = orders[0].OrderId,
                        ProductId = 1,
                        ProductName = "iPhone 15 Pro Max",
                        ProductImage = "/images/iphone15.jpg",
                        UnitPrice = 1200000,
                        Quantity = 1,
                        TotalPrice = 1200000
                    },
                    new OrderItem
                    {
                        OrderId = orders[0].OrderId,
                        ProductId = 2,
                        ProductName = "Ốp lưng iPhone",
                        ProductImage = "/images/oplung.jpg",
                        UnitPrice = 300000,
                        Quantity = 1,
                        TotalPrice = 300000
                    },
                    new OrderItem
                    {
                        OrderId = orders[1].OrderId,
                        ProductId = 3,
                        ProductName = "Samsung S24 Ultra",
                        ProductImage = "/images/s24.jpg",
                        UnitPrice = 850000,
                        Quantity = 1,
                        TotalPrice = 850000
                    },
                    new OrderItem
                    {
                        OrderId = orders[2].OrderId,
                        ProductId = 4,
                        ProductName = "Tai nghe Bluetooth",
                        ProductImage = "/images/tainghe.jpg",
                        UnitPrice = 450000,
                        Quantity = 1,
                        TotalPrice = 450000
                    }
                };

                context.OrderItems.AddRange(orderItems);
                context.SaveChanges();
            }
        }
    }
}
