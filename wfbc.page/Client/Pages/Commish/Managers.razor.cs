using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;

namespace WFBC.Client.Pages.Commish
{
    public class ManagersModel : ComponentBase
    {
        [Inject] public AuthorizedClient AuthorizedClient { get; set; }
        [Inject] public PublicClient PublicClient { get; set; }
        [Inject] public NavigationManager UrlNavigationManager { get; set; }

        protected List<Manager> managers = new List<Manager>();
        protected Manager manager = new Manager();

        protected override async Task OnInitializedAsync() => await GetAllManagers();

        protected async Task GetAllManagers()
            => managers = await PublicClient.Client.GetFromJsonAsync<List<Manager>>("api/manager") ?? new();

        protected void DeleteConfirm(string id)
            => manager = managers.FirstOrDefault(m => m.Id == id) ?? new Manager();

        protected async Task DeleteManager(string managerId)
        {
            try { await AuthorizedClient.Client.DeleteAsync("api/manager/" + managerId); await GetAllManagers(); }
            catch (AccessTokenNotAvailableException e) { e.Redirect(); }
        }
    }
}