using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Data.Common;

namespace Shop.Models
{
    public static class SeedData
    {
        public static void EnsurePopulated(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if database tables already exist before running migrations
            bool tableExists = false;
            try
            {
                var connection = context.Database.GetDbConnection();
                var wasOpen = connection.State == System.Data.ConnectionState.Open;
                if (!wasOpen)
                    connection.Open();

                try
                {
                    using var command = connection.CreateCommand();
                    
                    // Check if Brands table exists (indicates migration was partially applied)
                    command.CommandText = @"
                        SELECT CASE 
                            WHEN EXISTS (SELECT * FROM sys.tables WHERE name = 'Brands') 
                            THEN 1 ELSE 0 
                        END";
                    tableExists = Convert.ToBoolean(command.ExecuteScalar());

                    // Ensure __EFMigrationsHistory table exists
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
                        BEGIN
                            CREATE TABLE [__EFMigrationsHistory] (
                                [MigrationId] nvarchar(150) NOT NULL,
                                [ProductVersion] nvarchar(32) NOT NULL,
                                CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                            );
                        END";
                    command.ExecuteNonQuery();

                    // If tables exist but migration is not recorded, mark it as applied
                    if (tableExists)
                    {
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251116175825_Init')
                            BEGIN
                                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                                VALUES ('20251116175825_Init', '7.0.0');
                            END";
                        command.ExecuteNonQuery();
                    }
                }
                finally
                {
                    if (!wasOpen)
                        connection.Close();
                }
            }
            catch
            {
                // If we can't check, continue anyway - migration will handle it
            }

            // Now try to run migrations (will skip if already applied)
            try
            {
                if (context.Database.GetPendingMigrations().Any())
                    context.Database.Migrate();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2714 || ex.Number == 2705) // Object already exists or Duplicate column name
            {
                // If migration still fails, mark it as applied
                try
                {
                    var connection = context.Database.GetDbConnection();
                    var wasOpen = connection.State == System.Data.ConnectionState.Open;
                    if (!wasOpen)
                        connection.Open();

                    try
                    {
                        using var command = connection.CreateCommand();
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
                            BEGIN
                                CREATE TABLE [__EFMigrationsHistory] (
                                    [MigrationId] nvarchar(150) NOT NULL,
                                    [ProductVersion] nvarchar(32) NOT NULL,
                                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                                );
                            END";
                        command.ExecuteNonQuery();

                        command.CommandText = @"
                            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251116175825_Init')
                            BEGIN
                                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                                VALUES ('20251116175825_Init', '7.0.0');
                            END";
                        command.ExecuteNonQuery();
                    }
                    finally
                    {
                        if (!wasOpen)
                            connection.Close();
                    }
                }
                catch
                {
                    // Ignore if we can't update migration history
                }
            }

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
                    new Category { Name = "Books", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Toys & Games", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Beauty & Personal Care", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Automotive", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Food & Beverages", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Category { Name = "Health & Fitness", IsActive = true, CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser }
                );
                context.SaveChanges();
            }

            // ========================= SEED PRODUCTS =========================
            if (!context.Products.Any())
            {
                var electronics = context.Categories.First(c => c.Name == "Electronics");
                var clothing = context.Categories.First(c => c.Name == "Clothing");
                var sports = context.Categories.First(c => c.Name == "Sports");
                var books = context.Categories.First(c => c.Name == "Books");
                var toys = context.Categories.First(c => c.Name == "Toys & Games");
                var beauty = context.Categories.First(c => c.Name == "Beauty & Personal Care");

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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Smart Watch",
                        Description = "Fitness tracker with heart rate monitor",
                        CategoryId = electronics.CategoryId,
                        Price = 199.99m,
                        SalePrice = 179.99m,
                        StockQuantity = 40,
                        IsFeatured = true,
                        IsActive = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Leather Jacket",
                        Description = "Classic leather jacket for all seasons",
                        CategoryId = clothing.CategoryId,
                        Price = 299.99m,
                        StockQuantity = 25,
                        IsFeatured = true,
                        IsActive = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Programming Book",
                        Description = "Complete guide to modern programming",
                        CategoryId = books.CategoryId,
                        Price = 49.99m,
                        StockQuantity = 80,
                        IsActive = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    },
                    new Product
                    {
                        Name = "Board Game Set",
                        Description = "Family board game collection",
                        CategoryId = toys.CategoryId,
                        Price = 79.99m,
                        StockQuantity = 35,
                        IsActive = true,
                        Image = "/images/home/anh.jpg",
                        CreatedAt = now,
                        CreatedBy = defaultUser,
                        UpdatedAt = now,
                        UpdatedBy = defaultUser
                    }
                );
                context.SaveChanges();
            }

            // ========================= SEED BRANDS =========================
            if (!context.Brands.Any())
            {
                context.Brands.AddRange(
                    new Brand { Name = "Apple", Slug = "apple", Image = "/images/brands/brands1.jpg", Description = "Technology brand", IsActive = true },
                    new Brand { Name = "Samsung", Slug = "samsung", Image = "/images/brands/brands1.jpg", Description = "Electronics manufacturer", IsActive = true },
                    new Brand { Name = "Nike", Slug = "nike", Image = "/images/brands/brands1.jpg", Description = "Sportswear brand", IsActive = true },
                    new Brand { Name = "Adidas", Slug = "adidas", Image = "/images/brands/brands1.jpg", Description = "Athletic apparel", IsActive = true },
                    new Brand { Name = "Sony", Slug = "sony", Image = "/images/brands/brands1.jpg", Description = "Electronics and entertainment", IsActive = true },
                    new Brand { Name = "LG", Slug = "lg", Image = "/images/brands/brands1.jpg", Description = "Consumer electronics", IsActive = true },
                    new Brand { Name = "Canon", Slug = "canon", Image = "/images/brands/brands1.jpg", Description = "Camera and imaging", IsActive = true },
                    new Brand { Name = "Dell", Slug = "dell", Image = "/images/brands/brands1.jpg", Description = "Computer technology", IsActive = true },
                    new Brand { Name = "HP", Slug = "hp", Image = "/images/brands/brands1.jpg", Description = "Technology solutions", IsActive = true },
                    new Brand { Name = "Microsoft", Slug = "microsoft", Image = "/images/brands/brands1.jpg", Description = "Software and hardware", IsActive = true }
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
                        OrderDate = DateTime.Now.AddDays(-10),
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
                        ShippedDate = DateTime.Now.AddDays(-9),
                        DeliveredDate = DateTime.Now.AddDays(-8)
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111002",
                        OrderDate = DateTime.Now.AddDays(-9),
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
                        OrderDate = DateTime.Now.AddDays(-8),
                        Status = "Pending",
                        SubTotal = 450000,
                        TaxAmount = 45000,
                        ShippingAmount = 15000,
                        TotalAmount = 510000,
                        CustomerName = "Lê Văn C",
                        CustomerPhone = "0933456789",
                        CustomerEmail = "c@example.com",
                        ShippingAddress = "78 Hai Bà Trưng, Quận 1, TP.HCM"
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111004",
                        OrderDate = DateTime.Now.AddDays(-7),
                        Status = "Shipped",
                        SubTotal = 1200000,
                        TaxAmount = 120000,
                        ShippingAmount = 25000,
                        TotalAmount = 1345000,
                        CustomerName = "Phạm Thị D",
                        CustomerPhone = "0944567890",
                        CustomerEmail = "d@example.com",
                        ShippingAddress = "12 Trần Hưng Đạo, Quận 5, TP.HCM",
                        ShippedDate = DateTime.Now.AddDays(-6)
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111005",
                        OrderDate = DateTime.Now.AddDays(-6),
                        Status = "Delivered",
                        SubTotal = 2300000,
                        TaxAmount = 230000,
                        ShippingAmount = 35000,
                        TotalAmount = 2565000,
                        CustomerName = "Hoàng Văn E",
                        CustomerPhone = "0955678901",
                        CustomerEmail = "e@example.com",
                        ShippingAddress = "56 Võ Văn Tần, Quận 3, TP.HCM",
                        ShippedDate = DateTime.Now.AddDays(-5),
                        DeliveredDate = DateTime.Now.AddDays(-4)
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111006",
                        OrderDate = DateTime.Now.AddDays(-5),
                        Status = "Processing",
                        SubTotal = 680000,
                        TaxAmount = 68000,
                        ShippingAmount = 18000,
                        TotalAmount = 766000,
                        CustomerName = "Nguyễn Thị F",
                        CustomerPhone = "0966789012",
                        CustomerEmail = "f@example.com",
                        ShippingAddress = "89 Nguyễn Đình Chiểu, Quận 3, TP.HCM"
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111007",
                        OrderDate = DateTime.Now.AddDays(-4),
                        Status = "Pending",
                        SubTotal = 920000,
                        TaxAmount = 92000,
                        ShippingAmount = 22000,
                        TotalAmount = 1034000,
                        CustomerName = "Trần Văn G",
                        CustomerPhone = "0977890123",
                        CustomerEmail = "g@example.com",
                        ShippingAddress = "34 Lý Tự Trọng, Quận 1, TP.HCM"
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111008",
                        OrderDate = DateTime.Now.AddDays(-3),
                        Status = "Shipped",
                        SubTotal = 1500000,
                        TaxAmount = 150000,
                        ShippingAmount = 30000,
                        TotalAmount = 1680000,
                        CustomerName = "Lê Thị H",
                        CustomerPhone = "0988901234",
                        CustomerEmail = "h@example.com",
                        ShippingAddress = "67 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM",
                        ShippedDate = DateTime.Now.AddDays(-2)
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111009",
                        OrderDate = DateTime.Now.AddDays(-2),
                        Status = "Delivered",
                        SubTotal = 540000,
                        TaxAmount = 54000,
                        ShippingAmount = 15000,
                        TotalAmount = 609000,
                        CustomerName = "Phạm Văn I",
                        CustomerPhone = "0999012345",
                        CustomerEmail = "i@example.com",
                        ShippingAddress = "23 Cách Mạng Tháng 8, Quận 10, TP.HCM",
                        ShippedDate = DateTime.Now.AddDays(-1),
                        DeliveredDate = DateTime.Now
                    },
                    new Order
                    {
                        OrderNumber = "ORD20251111010",
                        OrderDate = DateTime.Now.AddDays(-1),
                        Status = "Pending",
                        SubTotal = 780000,
                        TaxAmount = 78000,
                        ShippingAmount = 20000,
                        TotalAmount = 878000,
                        CustomerName = "Hoàng Thị K",
                        CustomerPhone = "0900123456",
                        CustomerEmail = "k@example.com",
                        ShippingAddress = "45 Xô Viết Nghệ Tĩnh, Quận Bình Thạnh, TP.HCM"
                    }
                };

                context.Orders.AddRange(orders);
                context.SaveChanges();

                // ========================= SEED ORDER ITEMS =========================
                var products = context.Products.Take(10).ToList();
                var orderItems = new List<OrderItem>
                {
                    new OrderItem { OrderId = orders[0].OrderId, ProductId = products[0].ProductID, ProductName = products[0].Name, ProductImage = products[0].Image ?? "", UnitPrice = products[0].Price, Quantity = 1, TotalPrice = products[0].Price },
                    new OrderItem { OrderId = orders[0].OrderId, ProductId = products[1].ProductID, ProductName = products[1].Name, ProductImage = products[1].Image ?? "", UnitPrice = products[1].Price, Quantity = 1, TotalPrice = products[1].Price },
                    new OrderItem { OrderId = orders[1].OrderId, ProductId = products[2].ProductID, ProductName = products[2].Name, ProductImage = products[2].Image ?? "", UnitPrice = products[2].Price, Quantity = 2, TotalPrice = products[2].Price * 2 },
                    new OrderItem { OrderId = orders[2].OrderId, ProductId = products[3].ProductID, ProductName = products[3].Name, ProductImage = products[3].Image ?? "", UnitPrice = products[3].Price, Quantity = 1, TotalPrice = products[3].Price },
                    new OrderItem { OrderId = orders[3].OrderId, ProductId = products[4].ProductID, ProductName = products[4].Name, ProductImage = products[4].Image ?? "", UnitPrice = products[4].Price, Quantity = 1, TotalPrice = products[4].Price },
                    new OrderItem { OrderId = orders[4].OrderId, ProductId = products[5].ProductID, ProductName = products[5].Name, ProductImage = products[5].Image ?? "", UnitPrice = products[5].Price, Quantity = 3, TotalPrice = products[5].Price * 3 },
                    new OrderItem { OrderId = orders[4].OrderId, ProductId = products[6].ProductID, ProductName = products[6].Name, ProductImage = products[6].Image ?? "", UnitPrice = products[6].Price, Quantity = 1, TotalPrice = products[6].Price },
                    new OrderItem { OrderId = orders[5].OrderId, ProductId = products[7].ProductID, ProductName = products[7].Name, ProductImage = products[7].Image ?? "", UnitPrice = products[7].Price, Quantity = 1, TotalPrice = products[7].Price },
                    new OrderItem { OrderId = orders[6].OrderId, ProductId = products[8].ProductID, ProductName = products[8].Name, ProductImage = products[8].Image ?? "", UnitPrice = products[8].Price, Quantity = 2, TotalPrice = products[8].Price * 2 },
                    new OrderItem { OrderId = orders[7].OrderId, ProductId = products[9].ProductID, ProductName = products[9].Name, ProductImage = products[9].Image ?? "", UnitPrice = products[9].Price, Quantity = 1, TotalPrice = products[9].Price },
                    new OrderItem { OrderId = orders[8].OrderId, ProductId = products[0].ProductID, ProductName = products[0].Name, ProductImage = products[0].Image ?? "", UnitPrice = products[0].Price, Quantity = 1, TotalPrice = products[0].Price },
                    new OrderItem { OrderId = orders[9].OrderId, ProductId = products[1].ProductID, ProductName = products[1].Name, ProductImage = products[1].Image ?? "", UnitPrice = products[1].Price, Quantity = 1, TotalPrice = products[1].Price }
                };

                context.OrderItems.AddRange(orderItems);
                context.SaveChanges();
            }

            // ========================= SEED SLIDERS =========================
            if (!context.Sliders.Any())
            {
                context.Sliders.AddRange(
                    new Slider { Title = "Khuyến mãi đặc biệt", Image = "/images/home/slider_1.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Sản phẩm mới nhất", Image = "/images/home/slider_2.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Giảm giá 50%", Image = "/images/home/slider_1.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Mua 2 tặng 1", Image = "/images/home/slider_2.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Flash Sale", Image = "/images/home/slider_1.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Sản phẩm bán chạy", Image = "/images/home/slider_2.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Mùa hè sôi động", Image = "/images/home/slider_1.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Quà tặng hấp dẫn", Image = "/images/home/slider_2.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Black Friday", Image = "/images/home/slider_1.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser },
                    new Slider { Title = "Cyber Monday", Image = "/images/home/slider_2.webp", CreatedAt = now, CreatedBy = defaultUser, UpdatedAt = now, UpdatedBy = defaultUser }
                );
                context.SaveChanges();
            }

            // ========================= SEED CONTACTS =========================
            if (!context.Contacts.Any())
            {
                context.Contacts.AddRange(
                    new Contact { Name = "Nguyễn Văn A", Email = "a@example.com", Phone = "0901234567", Subject = "Hỏi về sản phẩm", Message = "Tôi muốn biết thêm thông tin về sản phẩm", CreatedAt = now.AddDays(-10), IsRead = true, ReadAt = now.AddDays(-9) },
                    new Contact { Name = "Trần Thị B", Email = "b@example.com", Phone = "0912345678", Subject = "Đổi trả hàng", Message = "Tôi muốn đổi trả sản phẩm đã mua", CreatedAt = now.AddDays(-9), IsRead = true, ReadAt = now.AddDays(-8) },
                    new Contact { Name = "Lê Văn C", Email = "c@example.com", Phone = "0923456789", Subject = "Giao hàng", Message = "Khi nào tôi nhận được hàng?", CreatedAt = now.AddDays(-8), IsRead = false },
                    new Contact { Name = "Phạm Thị D", Email = "d@example.com", Phone = "0934567890", Subject = "Thanh toán", Message = "Tôi có thể thanh toán bằng cách nào?", CreatedAt = now.AddDays(-7), IsRead = true, ReadAt = now.AddDays(-6) },
                    new Contact { Name = "Hoàng Văn E", Email = "e@example.com", Phone = "0945678901", Subject = "Khuyến mãi", Message = "Có chương trình khuyến mãi nào không?", CreatedAt = now.AddDays(-6), IsRead = false },
                    new Contact { Name = "Nguyễn Thị F", Email = "f@example.com", Phone = "0956789012", Subject = "Bảo hành", Message = "Sản phẩm có bảo hành không?", CreatedAt = now.AddDays(-5), IsRead = true, ReadAt = now.AddDays(-4) },
                    new Contact { Name = "Trần Văn G", Email = "g@example.com", Phone = "0967890123", Subject = "Đánh giá", Message = "Tôi muốn đánh giá sản phẩm", CreatedAt = now.AddDays(-4), IsRead = false },
                    new Contact { Name = "Lê Thị H", Email = "h@example.com", Phone = "0978901234", Subject = "Hỗ trợ", Message = "Cần hỗ trợ về đơn hàng", CreatedAt = now.AddDays(-3), IsRead = true, ReadAt = now.AddDays(-2) },
                    new Contact { Name = "Phạm Văn I", Email = "i@example.com", Phone = "0989012345", Subject = "Liên hệ", Message = "Tôi muốn liên hệ với bộ phận bán hàng", CreatedAt = now.AddDays(-2), IsRead = false },
                    new Contact { Name = "Hoàng Thị K", Email = "k@example.com", Phone = "0990123456", Subject = "Câu hỏi", Message = "Tôi có một số câu hỏi về sản phẩm", CreatedAt = now.AddDays(-1), IsRead = false }
                );
                context.SaveChanges();
            }

            // ========================= SEED PRODUCT REVIEWS =========================
            if (!context.ProductReviews.Any())
            {
                var products = context.Products.Take(10).ToList();
                if (products.Any())
                {
                    var userIds = new[] { "user1", "user2", "user3", "user4", "user5", "user6", "user7", "user8", "user9", "user10" };
                    var userNames = new[] { "Nguyễn Văn A", "Trần Thị B", "Lê Văn C", "Phạm Thị D", "Hoàng Văn E", "Nguyễn Thị F", "Trần Văn G", "Lê Thị H", "Phạm Văn I", "Hoàng Thị K" };
                    
                    var reviews = new List<ProductReview>();
                    for (int i = 0; i < 10; i++)
                    {
                        // Đảm bảo không tạo duplicate reviews cho cùng sản phẩm và user
                        var product = products[i % products.Count];
                        var userId = userIds[i];
                        
                        // Kiểm tra xem đã có review của user này cho sản phẩm này chưa
                        var existingReview = context.ProductReviews
                            .FirstOrDefault(r => r.ProductId == product.ProductID && r.UserId == userId);
                        
                        if (existingReview == null)
                        {
                            reviews.Add(new ProductReview
                            {
                                // ReviewId sẽ được tự động generate (Identity)
                                ProductId = product.ProductID,
                                UserId = userId,
                                UserName = userNames[i],
                                Rating = (i % 5) + 1,
                                Comment = $"Đánh giá sản phẩm {i + 1}: Sản phẩm rất tốt, chất lượng cao, giao hàng nhanh.",
                                CreatedAt = now.AddDays(-(10 - i)),
                                IsActive = true
                            });
                        }
                    }
                    
                    if (reviews.Any())
                    {
                        context.ProductReviews.AddRange(reviews);
                        context.SaveChanges();
                    }
                }
            }

            // ========================= SEED CAMPING TIPS =========================
            if (!context.CampingTips.Any())
            {
                context.CampingTips.AddRange(
                    new CampingTip { Title = "Chọn địa điểm cắm trại phù hợp", Content = "Nên chọn nơi bằng phẳng, có nguồn nước gần, tránh khu vực nguy hiểm", Image = "/images/camping/camping-1.jpg", Author = "Admin", CreatedAt = now.AddDays(-10), IsPublished = true },
                    new CampingTip { Title = "Chuẩn bị lều trại đúng cách", Content = "Kiểm tra lều trước khi đi, mang theo dây cố định và bạt che mưa", Image = "/images/camping/camping-2.jpg", Author = "Admin", CreatedAt = now.AddDays(-9), IsPublished = true },
                    new CampingTip { Title = "An toàn khi đốt lửa trại", Content = "Chỉ đốt lửa ở khu vực được phép, luôn dập lửa kỹ trước khi rời đi", Image = "/images/camping/camping-3.jpg", Author = "Admin", CreatedAt = now.AddDays(-8), IsPublished = true },
                    new CampingTip { Title = "Bảo quản thực phẩm", Content = "Giữ thực phẩm trong hộp kín, tránh ánh nắng trực tiếp, sử dụng đá giữ lạnh", Image = "/images/camping/camping-1.jpg", Author = "Admin", CreatedAt = now.AddDays(-7), IsPublished = true },
                    new CampingTip { Title = "Xử lý rác thải", Content = "Mang theo túi rác, không vứt rác bừa bãi, để lại nơi cắm trại sạch sẽ", Image = "/images/camping/camping-2.jpg", Author = "Admin", CreatedAt = now.AddDays(-6), IsPublished = true },
                    new CampingTip { Title = "Chuẩn bị quần áo phù hợp", Content = "Mang theo quần áo ấm, áo mưa, giày đi bộ phù hợp với địa hình", Image = "/images/camping/camping-3.jpg", Author = "Admin", CreatedAt = now.AddDays(-5), IsPublished = true },
                    new CampingTip { Title = "Sử dụng bản đồ và la bàn", Content = "Luôn mang theo bản đồ khu vực, biết cách sử dụng la bàn", Image = "/images/camping/camping-1.jpg", Author = "Admin", CreatedAt = now.AddDays(-4), IsPublished = true },
                    new CampingTip { Title = "Sơ cứu cơ bản", Content = "Mang theo hộp sơ cứu, biết cách xử lý vết thương nhỏ, côn trùng cắn", Image = "/images/camping/camping-2.jpg", Author = "Admin", CreatedAt = now.AddDays(-3), IsPublished = true },
                    new CampingTip { Title = "Quan sát thời tiết", Content = "Kiểm tra dự báo thời tiết trước khi đi, chuẩn bị cho các tình huống thời tiết xấu", Image = "/images/camping/camping-1.jpg", Author = "Admin", CreatedAt = now.AddDays(-2), IsPublished = true },
                    new CampingTip { Title = "Tôn trọng thiên nhiên", Content = "Không làm tổn hại cây cối, động vật, giữ gìn môi trường tự nhiên", Image = "/images/camping/camping-2.jpg", Author = "Admin", CreatedAt = now.AddDays(-1), IsPublished = true }
                );
                context.SaveChanges();
            }
        }
    }
}
