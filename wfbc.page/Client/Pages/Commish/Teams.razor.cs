using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;
using MongoDB.Bson;

namespace WFBC.Client.Pages.Commish
{
    public class TeamsModel : CommishModel
    {
        protected void DeleteConfirm(string ID)
        {
            team = teams.FirstOrDefault(x => x.Id == ID);
        }
        protected async Task DeleteTeam(string teamID)
        {
            try
            {
                await AuthorizedClient.Client.DeleteAsync("api/team/" + teamID);
                await GetAllTeams();
            }
            catch (AccessTokenNotAvailableException e)
            {
                e.Redirect();
            }
        }
    }
}
