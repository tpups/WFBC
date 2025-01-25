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
        protected string Title = "Build";
        [Parameter]
        public string standingsID { get; set; }
        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(standingsID))
            {
                Title = "Update";
                standings = await AuthorizedClient.Client.GetFromJsonAsync<Standings>("/api/standings/" + standingsID);
            }
            else
            {
                standings = new Standings();
            }
        }
        protected async Task BuildStandings()
        {

        }
        protected async Task SaveStandings()
        {

        }
    }
}
