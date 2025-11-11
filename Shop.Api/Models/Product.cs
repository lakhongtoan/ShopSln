using System;
using System.Collections.Generic;

namespace Shop.Api.Models;

public partial class Product
{
    public long ProductID { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Image { get; set; }

    public int CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ImageGallery { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsFeatured { get; set; }

    public decimal? SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public int? BrandId { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<CartItem> CartItems { get; } = new List<CartItem>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; } = new List<OrderItem>();
}
