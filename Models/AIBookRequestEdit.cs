using Newtonsoft.Json;

namespace EBookDashboard.Models
{
    public class AIBookRequestEdit
    {
        [JsonProperty("response_id")]
        public string? ResponseId { get; set; } = string.Empty;

        [JsonProperty("user_id")]
        public string? UserId { get; set; }

        [JsonProperty("book_id")]
        public string? BookId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;


        [JsonProperty("chapter")]
        public int Chapter { get; set; }   // 👈 Should be INT (not string)

        [JsonProperty("changes")]
        public string Changes { get; set; } = string.Empty;
    }
}
