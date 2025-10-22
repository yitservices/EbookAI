using Newtonsoft.Json;
using System.Xml.Linq;

namespace EBookDashboard.Models
{
    public class AIBookResponse
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        public AIData Data { get; set; }  // Add this Data property

        [JsonProperty("chapters")]
        public List<AIChapterResponse> Chapters { get; set; }


    }
}

