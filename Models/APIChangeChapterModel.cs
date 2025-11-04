namespace EBookDashboard.Models
{
    public class APIChangeChapterModel
    {
        public string UserId { get; set; } = string.Empty;
        public string BookId { get; set; } = string.Empty;
        public string Chapter { get; set; } = string.Empty;
        public string NewContent { get; set; } = string.Empty;
    }
}


