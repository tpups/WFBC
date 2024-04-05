using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;

namespace WFBC.Client.Pages.Commish
{
    public class BuildUpdateStandingsModel : StandingsModel
    {
        protected string Title = "Add";
        [Parameter]
        public string teamID { get; set; }
        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(teamID))
            {
                Title = "Edit";
                team = await AuthorizedClient.Client.GetFromJsonAsync<Team>("/api/team/" + teamID);
            }
            else
            {
                team = new Team();
            }
        }
        protected async Task SaveTeam()
        {
            List<Manager> managers = await AuthorizedClient.Client.GetFromJsonAsync<List<Manager>>("/api/manager/");
            Manager oldManager = managers.Where(m => m.TeamId == team.Id).FirstOrDefault();
            if (oldManager != null)
            {
                oldManager.TeamId = null;
                await AuthorizedClient.Client.PutAsJsonAsync("/api/manager/", oldManager);
            }

            if (team.ManagerId == "")
            {
                team.ManagerId = null;
            }
            else
            {
                manager = await AuthorizedClient.Client.GetFromJsonAsync<Manager>("/api/manager/" + team.ManagerId);
                manager.TeamId = team.Id;
                await AuthorizedClient.Client.PutAsJsonAsync("/api/manager/", manager);
            }

            if (team.Id != null)
            {
                team.LastUpdatedAt = DateTime.Now;
                await AuthorizedClient.Client.PutAsJsonAsync("/api/team/", team);
                UrlNavigationManager.NavigateTo("/commish/teams");
            }
            else
            {
                team.CreatedAt = DateTime.Now;
                team.LastUpdatedAt = team.CreatedAt;
                await AuthorizedClient.Client.PostAsJsonAsync("/api/team/", team);
                UrlNavigationManager.NavigateTo("/commish/teams");
            }
        }
    }
}
