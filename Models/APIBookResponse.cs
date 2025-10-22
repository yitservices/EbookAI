using Newtonsoft.Json;

namespace EBookDashboard.Models
{
    public class APIBookResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("data")]
        public AIData Data { get; set; }  // Add this Data property
    }
}
