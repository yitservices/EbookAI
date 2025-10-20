using System.ComponentModel.DataAnnotations;

namespace EBookDashboard.Models
{
    public class Language
    {
        [Required]
        [Key]
        public int LanguageId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;
        [MaxLength(12)]
        public string Status { get; set; } = string.Empty;
        [MaxLength(100)]
        public bool IsActive { get; set; }
    }
}
