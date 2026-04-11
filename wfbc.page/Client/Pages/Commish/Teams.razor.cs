using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;

namespace WFBC.Client.Pages.Commish
{
    public class TeamsModel : ComponentBase
    {
        [Inject] public AuthorizedClient AuthorizedClient { get; set; }
        [Inject] public PublicClient PublicClient { get; set; }
        [Inject] public NavigationManager UrlNavigationManager { get; set; }

        protected List<Manager> managers = new List<Manager>();
        protected List<SeasonTeam> seasonTeams = new List<SeasonTeam>();
        protected SeasonTeam seasonTeam = new SeasonTeam();
        protected string selectedYear = DateTime.Now.Year.ToString();
        protected List<int> availableYears = new List<int>();

        protected override async Task OnInitializedAsync()
        {
            for (int y = DateTime.Now.Year; y >= 2019; y--) availableYears.Add(y);
            await GetAllManagers();
            await LoadTeams();
        }

        protected async Task GetAllManagers()
            => managers = await PublicClient.Client.GetFromJsonAsync<List<Manager>>("api/manager") ?? new();

        protected async Task LoadTeams()
            => seasonTeams = await PublicClient.Client.GetFromJsonAsync<List<SeasonTeam>>($"api/seasonteam/{selectedYear}") ?? new();

        protected async Task OnYearChanged(ChangeEventArgs e)
        {
            selectedYear = e.Value?.ToString() ?? selectedYear;
            await LoadTeams();
        }

        protected void DeleteConfirm(string id)
            => seasonTeam = seasonTeams.FirstOrDefault(t => t.Id == id) ?? new SeasonTeam();

        protected async Task DeleteSeasonTeam(string id)
        {
            try { await AuthorizedClient.Client.DeleteAsync($"api/seasonteam/{selectedYear}/{id}"); await LoadTeams(); }
            catch (AccessTokenNotAvailableException e) { e.Redirect(); }
        }
    }
}