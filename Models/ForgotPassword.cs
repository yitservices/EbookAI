using System.ComponentModel.DataAnnotations;

namespace EBookDashboard.Models
{
    public class ForgotPassword
    {
        [Required]
        public string UserEmail { get; set; } = string.Empty;
    }
}
