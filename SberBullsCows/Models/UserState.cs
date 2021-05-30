using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SberBullsCows.Models
{
    public record UserState
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("total_score")]
        public int TotalScore { get; set; }

        [JsonProperty("last_words")]
        public List<string> LastWords { get; set; } = new();
    }
}