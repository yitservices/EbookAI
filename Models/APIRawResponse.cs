using System.ComponentModel.DataAnnotations;

namespace EBookDashboard.Models
{
    public class APIRawResponse
    {
        [Key]
        public int ResponseId { get; set; }

        [Required]
        public string Endpoint { get; set; }

        [Required]
        public string RequestData { get; set; }

        [Required]
        public string ResponseData { get; set; }

        public int? UserId { get; set; }
        public int? BookId { get; set; }

        public string StatusCode { get; set; }
        public string ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Store the parsed book ID for reference
        public int? ParsedBookId { get; set; }

    }
}
