using System.ComponentModel.DataAnnotations;
using System.Data;

namespace EBookDashboard.Models
{
    public class Users
    {
        [Required]
        [Key]
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string SecretQuestion {  get; set; } = string.Empty;
        public string SecretQuestionAnswer { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; }
        public int RoleId { get; set; }
        public string Status { get; set; } = string.Empty;
        public Roles Role { get; set; } // navigation property

    }
}
