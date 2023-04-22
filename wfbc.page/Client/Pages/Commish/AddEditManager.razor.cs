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
    public class AddEditManagerModel : ManagersModel
    {
        protected string Title = "Add";
        [Parameter]
        public string managerID { get; set; }
        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(managerID))
            {
                Title = "Edit";
                manager = await AuthorizedClient.Client.GetFromJsonAsync<Manager>("/api/manager/" + managerID);
                //manager = managers.FirstOrDefault(x => x.Id == manId);
            }
            else
            {
                manager = new Manager();
            }
        }
        protected async Task SaveManager()
        {
            if (manager.Id != null)
            {
                manager.LastUpdatedAt = DateTime.Now;
                await AuthorizedClient.Client.PutAsJsonAsync("/api/manager/", manager);
                UrlNavigationManager.NavigateTo("/commish/managers");
            }
            else
            {
                manager.CreatedAt = DateTime.Now;
                manager.LastUpdatedAt = manager.CreatedAt;
                await AuthorizedClient.Client.PostAsJsonAsync("/api/manager/", manager);
                UrlNavigationManager.NavigateTo("/commish/managers");
            }
        }
    }
}
