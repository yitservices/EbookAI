using System.ComponentModel.DataAnnotations;

namespace EBookDashboard.Models
{
    public class Categories
    {
        [Required]
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<Books> Books { get; set; } = new List<Books>();
    }
}
