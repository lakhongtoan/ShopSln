using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    public class Slider
    {
        public int SliderID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string Title { get; set; } = ""; // optional
        public string Image { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
