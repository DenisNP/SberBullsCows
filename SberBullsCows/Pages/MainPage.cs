using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using SberBullsCows.Helpers;
using SberBullsCows.Models;
using SberBullsCows.Models.Salute;

namespace SberBullsCows.Pages
{
    public class MainPage : ComponentBase
    {
        [Inject]
        public IJSRuntime Js { get; set; }
        public bool Informal { get; set; }

        public SessionState State { get; set; } = new();

        public List<WordSaid> WordsOrdered
        {
            get
            {
                if (State.WordsSaid.Count <= 1)
                    return State.WordsSaid;

                WordSaid last = State.WordsSaid.Last();
                return State.WordsSaid
                    .Take(State.WordsSaid.Count - 1)
                    .OrderByDescending(ws => ws.Bulls * 2 + ws.Cows)
                    .Prepend(last)
                    .ToList();
            }
        }

        public string CurrentWordShown => State.CurrentWord.IsNullOrEmpty()
            ? ""
            : (State.GameStarted ? "-----"[..State.CurrentWord.Length] : State.CurrentWord.ToUpper());

        public string ScoreText => State.ScoreGot == 0 ? "" : $"+{State.ScoreGot.ToPhrase("очко", "очка", "очков")}";
        
        public bool RulesOpened { get; set; } = false;

        protected override Task OnInitializedAsync()
        {
            Js.InvokeVoidAsync("initializeClient", Program.GetClientToken(), DotNetObjectReference.Create(this));
            return base.OnInitializedAsync();
        }

        [JSInvokable]
        public void CommandGot(string commandRaw)
        {
            if (Program.LogEnabled)
                Console.WriteLine(commandRaw);
            
            var command = JsonConvert.DeserializeObject<SmartAppCommand>(commandRaw, new SmartAppCommandConverter());
            if (command?.Type == "smart_app_data")
            {
                if (command.Data.ContainsKey("state"))
                {
                    var stateString = command.Data["state"].ToString();
                    State = !string.IsNullOrEmpty(stateString) ? JsonConvert.DeserializeObject<SessionState>(stateString) : null;
                    RulesOpened = false;
                    StateHasChanged();
                }
                else if (command.Data.ContainsKey("show_rules"))
                {
                    RulesOpened = true;
                    StateHasChanged();
                }
                else if (command.Data.ContainsKey("close"))
                {
                    Close();
                }
            } 
            else if (command?.Type == "character")
            {
                Informal = command.Character == "joy";
                StateHasChanged();
            }
        }

        public void StartGame(bool isHard = false)
        {
            SendData(isHard ? "start_hard" : "start_lite", new Dictionary<string, object>(), true);
        }
        
        protected void SendData(string action, Dictionary<string, object> data, bool enableCallback = false)
        {
            Js.InvokeVoidAsync("sendData", action, data, enableCallback);
        }

        public void ShowHideRules()
        {
            RulesOpened = !RulesOpened;
            if (RulesOpened)
                SendData("tell_rules", new Dictionary<string, object>());
        }

        public string GetTail(string s)
        {
            if (s.Length <= 3) return s;
            return s.Substring(0, 2) + "…";
        }

        protected void Close()
        {
            Js.InvokeVoidAsync("close");
        }
    }
}