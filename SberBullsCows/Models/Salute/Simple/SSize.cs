using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SberBullsCows.Models.Salute.Simple
{
    public class SSize
    {
        [JsonProperty("width")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SizeType Width { get; set; } = SizeType.medium;
        
        [JsonProperty("height")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SizeType Height { get; set; } = SizeType.medium;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum SizeType
    {
        small,
        medium,
        large,
        resizable
    }
}