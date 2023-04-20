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
        public string manId { get; set; }
        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(manId))
            {
                Title = "Edit";
                man = await AuthorizedClient.Client.GetFromJsonAsync<Manager>("/api/manager/" + manId);
                //man = manList.FirstOrDefault(x => x.Id == manId);
            }
            else
            {
                man = new Manager();
            }
        }
        protected async Task SaveManager()
        {
            if (man.Id != null)
            {
                await AuthorizedClient.Client.PutAsJsonAsync("/api/manager/", man);
                Navigate("/commish/managers");
            }
            else
            {
                await AuthorizedClient.Client.PostAsJsonAsync("/api/manager/", man);
                Navigate("/commish/managers");
            }
        }
        public void Navigate(string path = "")
        {
            UrlNavigationManager.NavigateTo(path);
        }
    }
}
