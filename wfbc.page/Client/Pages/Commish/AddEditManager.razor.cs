using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;

namespace WFBC.Client.Pages
{
    public class AddEditManagerModel : CommishModel
    {
        protected string Title = "Add";
        [Parameter]
        public string manId { get; set; }
        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(manId))
            {
                Title = "Edit";
                man = await Http.GetFromJsonAsync<Manager>("/api/manager/" + manId);
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
                await Http.PutAsJsonAsync("/api/manager/", man);
            }
            else
            {
                await Http.PostAsJsonAsync("/api/manager/", man);
            }
            ToCommish();
        }
        public void ToCommish()
        {
            UrlNavigationManager.NavigateTo("/commish");
        }
    }
}
