using Newtonsoft.Json;

namespace EBookDashboard.Models
{
    public class AIChapterResponse
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

    }
}
