using System;
using System.Collections.Generic;
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

        public void ShowHideRules()
        {
            RulesOpened = !RulesOpened;
        }

        protected void Close()
        {
            Js.InvokeVoidAsync("close");
        }
    }
}