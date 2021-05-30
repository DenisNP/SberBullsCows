using System.Collections.Generic;

namespace SberBullsCows.Models
{
    public class SessionState
    {
        public bool GameStarted { get; set; }
        public string CurrentWord { get; set; } = "";
        public int ScoreGot { get; set; }
        public List<WordSaid> WordsSaid { get; set; } = new();
    }
}