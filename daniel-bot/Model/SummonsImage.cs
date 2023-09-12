using System.Text.Json.Serialization;

namespace daniel_bot.Model
{
    public class SummonsImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}