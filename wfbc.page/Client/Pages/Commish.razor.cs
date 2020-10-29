using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;

namespace wfbc.page.Client.Pages
{
    public class CommishDataModel : ComponentBase
    {
        [Inject]
        public HttpClient Http { get; set; }
        protected List<Manager> manList = new List<Manager>(); 
        protected Manager man = new Manager();
        protected override async Task OnInitializedAsync()
        {
            await GetAllManagers();
        }
        protected async Task AddManager()
        {
            await Http.PostAsJsonAsync("api/manager", man);
        }
        protected async Task GetAllManagers()
        {
            manList = await Http.GetFromJsonAsync<List<Manager>>("api/manager");
        }
        protected void DeleteConfirm(string ID)
        {
            man = manList.FirstOrDefault(x => x.Id == ID);
        }
        protected async Task DeleteManager(string manID)
        {
            await Http.DeleteAsync("api/wfbc/managers" + manID);
            await GetAllManagers();
        }
    }
}
