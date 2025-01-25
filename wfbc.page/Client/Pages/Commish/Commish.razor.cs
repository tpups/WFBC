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
        protected List<Team> teams = new List<Team>();
        protected Team team = new Team();
        protected List<Standings> allStandings = new List<Standings>();
        protected Standings standings = new Standings();
        protected override async Task OnInitializedAsync()
        {
            await GetAllManagers();
            await GetAllDrafts();
            await GetAllTeams();
        }

        protected async Task GetAllManagers()
        {
            managers = await PublicClient.Client.GetFromJsonAsync<List<Manager>>("api/manager");
        }
        protected async Task GetAllDrafts()
        {
            drafts = await PublicClient.Client.GetFromJsonAsync<List<Draft>>("api/draft");
        }
        protected async Task GetAllTeams()
        {
            teams = await PublicClient.Client.GetFromJsonAsync<List<Team>>("api/team");
        }
    }
}
