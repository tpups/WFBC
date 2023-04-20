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
        public AuthorizedClient AuthorizedClient { get; set; }
        [Inject]
        public PublicClient PublicClient { get; set; }
        [Inject]
        public NavigationManager UrlNavigationManager { get; set; }
        protected List<Manager> managers = new List<Manager>();
        protected Manager manager = new Manager();
        protected List<Draft> drafts = new List<Draft>();
        protected Draft draft = new Draft();
        protected override async Task OnInitializedAsync()
        {
            await GetAllManagers();
        }

        protected async Task GetAllManagers()
        {
            managers = await PublicClient.Client.GetFromJsonAsync<List<Manager>>("api/manager");
        }
        protected async Task GetAllDrafts()
        {
            drafts = await PublicClient.Client.GetFromJsonAsync<List<Draft>>("api/draft");
        }
    }
}
