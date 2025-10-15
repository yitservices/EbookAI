namespace EBookDashboard.Models
{
    public class ResetPassword
    {
        public string UserEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

    }
}
