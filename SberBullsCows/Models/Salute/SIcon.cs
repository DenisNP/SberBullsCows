using Newtonsoft.Json;
using SberBullsCows.Models.Salute.Simple;

namespace SberBullsCows.Models.Salute
{
    public class SIcon
    {
        [JsonProperty("address")]
        public IconAddress Address { get; set; }

        [JsonProperty("size")]
        public SSize Size { get; }

        [JsonProperty("margins")]
        public Edges Margins { get; set; }

        public SIcon(string url, string hash, SizeType size)
        {
            Address = new IconAddress(url, hash);
            Size = new SSize
            {
                Width = size,
                Height = size
            };
        }
    }

    public class IconAddress
    {
        [JsonProperty("type")]
        public string Type { get; } = "url";

        [JsonProperty("url")]
        public string Url { get; }
        
        [JsonProperty("hash")]
        public string Hash { get; }

        public IconAddress(string url, string hash)
        {
            Url = url;
            Hash = hash;
        }
    }
}