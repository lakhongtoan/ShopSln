using System.ComponentModel.DataAnnotations;

namespace Shop.Models
{
    public class Brand
    {
        [Key]
        public int Id { get; set; }   // KHÓA CHÍNH LÀ Id

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? Image { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
