using Newtonsoft.Json;

namespace EBookDashboard.Models
{
    public class AIBookResponseEdit
    {
        [JsonProperty("title")]
        public string? Title { get; set; } = string.Empty;
        public string Status { get; set; } = "Raw";
        public AIData? Data { get; set; }  // Add this Data property

        [JsonProperty("chapters")]
        public List<AIChapterResponse> Chapters { get; set; }

    }
}
