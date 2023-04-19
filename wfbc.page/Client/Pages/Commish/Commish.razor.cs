using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;


namespace WFBC.Client.Pages.Commish
{
    public class CommishModel : ComponentBase
    {
        [Inject]
        public AuthorizedClient Http { get; set; }
        [Inject]
        public PublicClient PublicClient { get; set; }
        [Inject]
        public NavigationManager UrlNavigationManager { get; set; }
        protected List<Manager> manList = new List<Manager>();
        protected Manager man = new Manager();
        protected override async Task OnInitializedAsync()
        {
            await GetAllManagers();
        }

        protected async Task GetAllManagers()
        {
            manList = await PublicClient.Client.GetFromJsonAsync<List<Manager>>("api/manager");
        }
    }
}
