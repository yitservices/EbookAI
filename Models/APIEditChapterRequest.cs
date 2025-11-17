namespace EBookDashboard.Models
{
    public class APIEditChapterRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string BookId { get; set; } = string.Empty;
        public string Chapter { get; set; } = string.Empty;
        public string Changes { get; set; } = string.Empty;
    }
}


