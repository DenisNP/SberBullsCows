using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SberBullsCows.Helpers;

namespace SberBullsCows.Services
{
    public class ContentService
    {
        private readonly ILogger<ContentService> _logger;
        
        private string[] WordsHard { get; set; }
        private string[] WordsLite { get; set; }

        public ContentService(ILogger<ContentService> logger)
        {
            _logger = logger;
        }

        public void Load()
        {
            WordsLite = File.ReadAllLines(@"Data/words_lite.txt");
            _logger?.LogInformation($"Lite words loaded: {WordsLite.Length}");
            
            WordsHard = File.ReadAllLines(@"Data/words_hard.txt");
            _logger?.LogInformation($"Hard words loaded: {WordsHard.Length}");
        }

        public string GetNew(List<string> exclude, bool isHard = false)
        {
            string[] source = isHard ? WordsHard : WordsLite;
            var attempts = 500;
            string word = source.PickRandom();
            while (attempts-- > 0 && exclude.Contains(word)) 
                word = source.PickRandom();

            return word;
        }
    }
}