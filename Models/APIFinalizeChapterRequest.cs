namespace EBookDashboard.Models
{
    public class APIFinalizeChapterRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string BookId { get; set; } = string.Empty;
        public string Chapter { get; set; } = string.Empty;
        public bool Approve { get; set; } = false;
    }
}


