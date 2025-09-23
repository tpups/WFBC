using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;


namespace WFBC.Client.Pages.Commish
{
    public class ManagersModel : ComponentBase
    {
        [Inject]
        public AuthorizedClient AuthorizedClient { get; set; }
        [Inject]
        public PublicClient PublicClient { get; set; }
        [Inject]
        public NavigationManager UrlNavigationManager { get; set; }
        
        protected List<Manager> managers = new List<Manager>();
        protected Manager manager = new Manager();
        protected List<Team> teams = new List<Team>();

        protected override async Task OnInitializedAsync()
        {
            await GetAllManagers();
            await GetAllTeams();
        }

        protected async Task GetAllManagers()
        {
            managers = await PublicClient.Client.GetFromJsonAsync<List<Manager>>("api/manager");
        }

        protected async Task GetAllTeams()
        {
            teams = await PublicClient.Client.GetFromJsonAsync<List<Team>>("api/team");
        }

        protected void DeleteConfirm(string ID)
        {
            manager = managers.FirstOrDefault(x => x.Id == ID);
        }
        
        protected async Task DeleteManager(string managerID)
        {
            try
            {
                await AuthorizedClient.Client.DeleteAsync("api/manager/" + managerID);
                await GetAllManagers();
            }
            catch (AccessTokenNotAvailableException e)
            {
                e.Redirect();
            }
            catch
            {

            }
        }
    }
}
