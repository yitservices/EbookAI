namespace EBookDashboard.Models.DTO
{
    public class ChapterDto
    {
        public int ChapterNumber { get; set; }
        public string? Title { get; set; } = string.Empty;
        public string? Content { get; set; } = string.Empty;
        public string? StatusCode { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }=DateTime.Now;

    }
}
