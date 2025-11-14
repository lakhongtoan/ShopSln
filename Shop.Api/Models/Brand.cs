using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shop.Api.Models;

public partial class Brand
{
    public int BrandId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Slug { get; set; }

    public bool IsActive { get; set; }

    [JsonIgnore]
    public virtual ICollection<Product> Products { get; } = new List<Product>();
}
