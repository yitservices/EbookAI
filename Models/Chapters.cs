using System.ComponentModel.DataAnnotations;

namespace EBookDashboard.Models
{
    public class Chapters
    {
        [Required]
        [Key]
        public int ChapterId { get; set; }

        [Required]
        public int BookId { get; set; }
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty; // the actual chapter text (can be large)
        [Required]
        public int LanguageId { get; set; }

        public int OrderIndex { get; set; } // chapter number / order
        public int WordCount { get; set; } // chapter number / order

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int UpdatedByUserId { get; set; }
        public bool IsPublished { get; set; } = false;
        public string Status { get; set; } = string.Empty;
    }

}
