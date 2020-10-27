using System.Text.Json.Serialization;

namespace chanDL
{
    public class Thread
    {
        [JsonPropertyName("posts")]
        public Post[] Posts { get; set; }
    }
}
