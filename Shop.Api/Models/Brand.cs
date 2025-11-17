using System;
using System.Collections.Generic;

namespace Shop.Api.Models
{
    public partial class Brand
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Image { get; set; }

        public bool IsActive { get; set; } = true;

        // Đây là chỗ quan trọng để FIX lỗi bạn gặp
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
