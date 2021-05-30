using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SberBullsCows.Abstract;
using SberBullsCows.Helpers;
using SberBullsCows.Models;
using SberBullsCows.Models.Salute.Web;

namespace SberBullsCows.Services
{
    public class SaluteService
    {
        private const int StartingScore = 20;
        private const int AdditionalScore = 5;
        private const int WordsToStore = 50;
        
        private readonly IStateStorage<UserState> _userStateStorage;
        private readonly IStateStorage<SessionState> _sessionStateStorage;
        private readonly ContentService _contentService;

        public SaluteService(
            IStateStorage<UserState> userStateStorage,
            IStateStorage<SessionState> sessionStateStorage,
            ContentService contentService
        )
        {
            _userStateStorage = userStateStorage;
            _sessionStateStorage = sessionStateStorage;
            _contentService = contentService;
        }

        public SaluteResponse Handle(SaluteRequest request)
        {
            UserState user = _userStateStorage.GetState(request.UserId);
            SessionState session = _sessionStateStorage.GetState(request.UserId);
            var response = new SaluteResponse(request);

            // enter
            if (request.IsEnter)
                HandleEnter(request, response, user);

            else if (
                request.HasWords("помощь", "что уметь", "помогать", "помочь", "правило", "рассказать правило",
                    "правило игра", "показать правило", "открыть правило")
            )
                HandleHelp(response, session);
            
            else if (request.HasWords("выход", "выйти", "закрыть", "закрой"))
                HandleExit(request, response);
            
            else if (!session.GameStarted) // start new game
            {
                if (
                    request.HasWords("сложный игра", "начать сложный игра", "новый сложный игра", "сложный")
                    || request.HasCommand("start_hard")
                )
                    StartGame(request, response, user, session, true);

                else if (
                    request.HasWords("простой игра", "начать простой игра", "новый простой игра", "новый игра", "простой")
                    || request.HasCommand("start_lite")
                )
                    StartGame(request, response, user, session, false);

                else
                    HandleDontKnow(request, response);
            }
            else // game started, got new guess from player
            {
                HandleNewWord(request, response, session, user);
            }
            
            return response;
        }

        private void HandleNewWord(SaluteRequest request, SaluteResponse response, SessionState session, UserState user)
        {
            if (request.Tokens.Length == 0)
                return; // empty response
            
            string word = request.Tokens.First().ToLower();
            string currentWord = session.CurrentWord;
            
            // check if game is finished
            if (word == currentWord)
            {
                FinishGame(request, response, session, user);
                return;
            }
            
            // game is not finished, count cows and bulls
            var bulls = 0;
            var cows = 0;
            
            for (var i = 0; i < Math.Min(currentWord.Length, word.Length); i++)
            {
                char letter = word[i];
                if (currentWord[i] == letter)
                {
                    bulls++;
                } 
                else if (currentWord.Contains(letter))
                {
                    cows++;
                }
            }

            // save to session
            session.WordsSaid.Add(new WordSaid
            {
                Word = word,
                Cows = cows,
                Bulls = bulls
            });
            _sessionStateStorage.SetState(request.UserId, session);
            
            // generate response
            string cowsText = cows switch
            {
                1 => "одна корова",
                2 => "две коровые",
                _ => cows.ToPhrase("корова", "коровы", "коров")
            };

            response
                .AppendSendData("state", JsonConvert.SerializeObject(session))
                .AppendText(
                    $"В слове {word} {cowsText} и " +
                    $"{bulls.ToPhrase("бык", "быка", "быков")}."
                )
                .AppendSuggestions("Правила", "Выход");
            
            response.Payload.AutoListening = true;
        }

        private void FinishGame(SaluteRequest request, SaluteResponse response, SessionState session, UserState user)
        {
            int score = Math.Max(1, StartingScore - session.WordsSaid.Count);
            var addScore = false;
            if (session.WordsSaid.All(w => w.Word.Length == session.CurrentWord.Length))
            {
                addScore = true;
                score += AdditionalScore;
            }

            user.TotalScore += score;
            user.LastWords.Add(session.CurrentWord);
            while (user.LastWords.Count > WordsToStore) 
                user.LastWords.RemoveAt(0);
            
            session.ScoreGot = score;
            session.GameStarted = false;
            
            _sessionStateStorage.SetState(request.UserId, session);
            _userStateStorage.SetState(request.UserId, user);

            string stepsText = session.WordsSaid.Count.ToPhrase("попытку", "попытки", "попыток");
            string scoreText = score.ToPhrase("очко", "очка", "очков");
            string addScoreText = addScore
                ? $", из них дополнительно {AdditionalScore.ToPhrase("очко", "очка", "очков")} за использование " +
                  $"только слов одинаковой длины"
                : "";
            
            // generate response
            response
                .AppendText(request, new Phrase(
                    $"Отлично! Вы правильно догадались за {stepsText} и получаете {scoreText}{addScoreText}. " +
                    $"Если хотите, можете начать новую простую или сложную игру.",
                    $"Верно! Вы правильно отгадали слово за {stepsText} и получаете {scoreText}{addScoreText}. " +
                    $"Можете начать новую простую или сложную игру.",
                    $"Ура! Ты правильно догадался за {stepsText} и получаешь {scoreText}{addScoreText}. " +
                    $"Если хочешь, можешь начать новую простую или сложную игру."
                ))
                .AppendSendData("state", JsonConvert.SerializeObject(session))
                .AppendSuggestions("Простая игра", "Сложная игра", "Правила", "Выход");
            
            response.Payload.AutoListening = true;
        }

        private void HandleDontKnow(SaluteRequest request, SaluteResponse response)
        {
            response
                .AppendText(
                    request,
                    new Phrase(
                        "Скажите, какую игру хотите начать: простую или сложную. Можете попросить меня рассказать правила.",
                        "Выберите сложность для начала игру: простую или сложную. Либо я могу рассказать правила.",
                        "Какую игру хочешь начать, простую или сложную? Можешь также попросить меня рассказать правила!"
                    )
                )
                .AppendSuggestions("Простая игра", "Сложная игра", "Правила", "Выход");
        }

        private void StartGame(SaluteRequest request, SaluteResponse response, UserState user, SessionState session, bool isHard)
        {
            string word = _contentService.GetNew(user.LastWords, isHard);
            session.GameStarted = true;
            session.WordsSaid = new List<WordSaid>();
            session.ScoreGot = 0;
            session.CurrentWord = word;
            
            _sessionStateStorage.SetState(request.UserId, session);
            
            // generate response
            string lettersText = word.Length.ToPhrase("буквы", "букв", "букв");
            response
                .AppendSendData("state", JsonConvert.SerializeObject(session))
                .AppendText(request, new Phrase(
                    $"Начинаем новую игру. Я загадал слово из {lettersText}. Называйте мне своё слово в ответ, а я " +
                    $"буду говорить, сколько в нём быков и коров.",
                    $"Запускаю новую игру. Я загадала слово из {lettersText}. Называйте мне своё слово в ответ, а я " +
                    $"буду говорить, сколько в нём быков и коров.",
                    $"Начнём новую игру! Я загадала слово из {lettersText}. Называй мне своё слово в ответ, а я " +
                    $"буду говорить, сколько в нём быков и коров."
                ))
                .AppendSuggestions("Правила", "Выход");

            response.Payload.AutoListening = true;
        }

        private void HandleExit(SaluteRequest request, SaluteResponse response)
        {
            response
                .AppendText(request, new Phrase("Приходите ещё.", "Приходите ещё.", "Приходи ещё!"))
                .AppendSendData("close", "");

            response.Payload.Finished = true;
        }

        private void HandleHelp(SaluteResponse response, SessionState sessionState)
        {
            response
                .AppendText(
                    "Я загадываю слово, а игрок называет мне слова в ответ. Дальше считаем, сколько в его слове " +
                    "коров и быков. Коровы — это буквы, которые есть в обоих слова, но не " +
                    "стоят на своём месте. А быки — те буквы, которые полностью совпадают у двух слов! " +
                    "Таким образом, игрок должен вычислить загаданное слово по буквам."
                )
                .AppendSendData("show_rules", "");

            if (!sessionState.GameStarted)
                response.AppendSuggestions("Простая игра", "Сложная игра");
            
            response.Payload.AutoListening = true;
        }

        private void HandleEnter(SaluteRequest request, SaluteResponse response, UserState user)
        {
            var session = new SessionState();
            _sessionStateStorage.SetState(request.UserId, session);
                
            response
                .AppendText(
                    request,
                    new Phrase(
                        "Здравствуйте. Это игра «Быки и коровы» со словами. Если знаете, как играть, можете начать " +
                        "простую или сложную игру. Либо я расскажу вам правила.",
                        "Здравствуйте. Это игра «Быки и коровы» со словами. Если знаете, как играть, выберите " +
                        "простую или сложную игру. Либо я расскажу вам правила.",
                        "Привет! Это игра «Быки и коровы» со словами. Если знаешь, как играть, начинай " +
                        "простую или сложную игру. Либо я расскажу тебе правила."
                    )
                )
                .AppendSendData("state", JsonConvert.SerializeObject(session))
                .AppendSuggestions("Простая игра", "Сложная игра", "Правила", "Выход");

            response.Payload.AutoListening = true;
        }
    }
}