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

            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product
                    {
                        Name = "Kayak",
                        Description =
                            "A boat for one person",
                        Category = "Watersports",
                        Price = 275,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Lifejacket",
                        Description = "Protective and fashionable",
                        Category = "Watersports",
                        Price = 48.95m,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Soccer Ball",
                        Description = "FIFA-approved size and weight",
                        Category = "Soccer",
                        Price = 19.50m,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Corner Flags",
                        Description =
                          "Give your playing field a professional touch",
                        Category = "Soccer",
                        Price = 34.95m,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Stadium",
                        Description = "Flat-packed 35,000-seat stadium",
                        Category = "Soccer",
                        Price = 79500,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Thinking Cap",
                        Description = "Improve brain efficiency by 75%",
                        Category = "Chess",
                        Price = 16,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Unsteady Chair",
                        Description =
                          "Secretly give your opponent a disadvantage",
                        Category = "Chess",
                        Price = 29.95m,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Human Chess Board",
                        Description = "A fun game for the family",
                        Category = "Chess",
                        Price = 75,
                        Image = "/images/home/anh.jpg"
                    },
                    new Product
                    {
                        Name = "Bling-Bling King",
                        Description = "Gold-plated, diamond-studded King",
                        Category = "Chess",
                        Price = 1200,
                        Image = "/images/home/anh.jpg"
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
