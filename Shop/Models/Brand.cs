using System.ComponentModel.DataAnnotations;

namespace Shop.Models
{
    public class Brand
    {
        public int BrandId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(300)]
        public string? Image { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}