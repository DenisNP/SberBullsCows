using Newtonsoft.Json;
using SberBullsCows.Models.Salute.Simple;

namespace SberBullsCows.Models.Salute
{
    public class SGalleryItem
    {
        [JsonProperty("type")]
        public string Type { get; } = "media_gallery_item";

        [JsonProperty("image")]
        public Image Image { get; }
        
        [JsonProperty("margins")]
        public Edges Margins { get; set; }

        [JsonProperty("top_text")]
        public TextBlock TopText { get; set; }

        [JsonProperty("bottom_text")]
        public TextBlock BottomText { get; set; }

        public SGalleryItem(string url, string hash, ImageSize size)
        {
            Image = new Image(url, hash, size);
        }
    }
}