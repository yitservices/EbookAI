using System.ComponentModel.DataAnnotations;

namespace EBookDashboard.Models
{
    public class Settings
    {
        [Key]
        public int SettingId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Value { get; set; }
        
        [MaxLength(50)]
        public string Category { get; set; } = "General"; // General, Payment, Email, Security
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

