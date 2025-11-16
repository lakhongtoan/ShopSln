using System.ComponentModel.DataAnnotations;

namespace Shop.Models
{
    public class CampingTip
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Image { get; set; }    

        [StringLength(200)]
        public string? Author { get; set; }   

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsPublished { get; set; } = true;
    }
}