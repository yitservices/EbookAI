using Newtonsoft.Json;

namespace EBookDashboard.Models
{
    public class AIData
    {
        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("suggest_chapter_name")]
        public string SuggestChapterName { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("highlights")]
        public string Highlights { get; set; }

    }
}
