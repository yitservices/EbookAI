using System.ComponentModel.DataAnnotations;

namespace EBookDashboard.Models
{
    public class Roles
    {
        [Required]
        [Key]
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool AllowDownloads { get; set; }
        public bool AllowFullDashboard { get; set; }
        public bool AllowAnalytics { get; set; }
        public bool AllowPublishing { get; set; }
        public bool AllowDelete { get; set; }
        public bool AllowEdit { get; set; }
        [Required]
        public ICollection<Users> Users { get; set; }
    }
}
