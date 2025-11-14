using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shop.Api.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    [JsonIgnore]
    public virtual ICollection<Product> Products { get; } = new List<Product>();
}
