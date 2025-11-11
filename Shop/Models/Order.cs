using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required, StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        [StringLength(200)]
        public string CustomerPhone { get; set; } = string.Empty;

        [StringLength(200)]
        public string CustomerEmail { get; set; } = string.Empty;

        [StringLength(300)]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(300)]
        public string? BillingAddress { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }


        // Navigation property
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
