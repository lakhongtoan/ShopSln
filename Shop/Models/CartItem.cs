using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        // Tham chiếu đến sản phẩm
        [Required]
        public long ProductId { get; set; }

        [Required, StringLength(450)]
        public string? UserId { get; set; } = null;

        [Required, StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ProductImage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // SessionId để lưu tạm theo người dùng chưa login
        [Required, StringLength(50)]
        public string SessionId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual Product Product { get; set; } = null!;
    }
}
