using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookDashboard.Models
{
    [Table("recordstatus")]
    public class RecordStatus
    {
        [Required]
        [Key]
        public int Id { get; set; }
        [MaxLength(12)]
        public string Status { get; set; } = string.Empty;
        [MaxLength(150)]
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set;  } = DateTime.UtcNow;
        public bool IsActive { get; set; }
    }
}
