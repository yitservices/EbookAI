using Newtonsoft.Json;

namespace EBookDashboard.Models
{
    public class AISingleChapterData
    {
        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("suggest_chapter_name")]
        public string Title { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("highlights")]
        public string Highlights { get; set; }
    }
}
