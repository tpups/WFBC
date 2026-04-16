using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace WFBC.Client.Pages.Commish
{
    public class AddEditTeamModel : TeamsModel
    {
        protected string Title = "Add";

        [Parameter] public string? year { get; set; }
        [Parameter] public string? teamId { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(year)) selectedYear = year;
            await GetAllManagers();
            if (!string.IsNullOrEmpty(teamId))
            {
                Title = "Edit";
                seasonTeam = await PublicClient.Client.GetFromJsonAsync<SeasonTeam>($"api/seasonteam/{selectedYear}/{teamId}") ?? new SeasonTeam();
            }
            else
            {
                seasonTeam = new SeasonTeam { Year = int.Parse(selectedYear) };
            }
        }

        protected async Task SaveTeam()
        {
            if (!string.IsNullOrEmpty(seasonTeam.ManagerId))
            {
                var mgr = managers.FirstOrDefault(m => m.Id == seasonTeam.ManagerId);
                if (mgr != null)
                    seasonTeam.Manager = $"{mgr.FirstName} {mgr.LastName}".Trim();
            }
            seasonTeam.Year = int.Parse(selectedYear);

            if (!string.IsNullOrEmpty(seasonTeam.Id))
                await AuthorizedClient.Client.PutAsJsonAsync($"api/seasonteam/{selectedYear}", seasonTeam);
            else
                await AuthorizedClient.Client.PostAsJsonAsync($"api/seasonteam/{selectedYear}", seasonTeam);

            UrlNavigationManager.NavigateTo("/commish/teams");
        }
    }
}