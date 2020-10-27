using System.Text.Json.Serialization;

namespace chanDL
{
    public class Post
    {
        [JsonPropertyName("ext")]
        public string Extenstion { get; set; }
        [JsonPropertyName("tim")]
        public ulong Filename { get; set; }
    }
}
